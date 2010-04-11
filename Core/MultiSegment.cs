using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
    public class MultiSegment : IRuleSegment
    {
        private readonly uint _minMatches;
        private readonly uint? _maxMatches;
        private readonly IEnumerable<IRuleSegment> _match;

        public MultiSegment(IEnumerable<IRuleSegment> match, uint minMatches, uint? maxMatches)
        {
            if (!match.All(s => s.IsMatchOnlySegment))
            {
                throw new ArgumentException("MultiSegment can only encapsulate match-only segments");
            }

            _match = match;
            _minMatches = minMatches;
            _maxMatches = maxMatches;
        }

        private bool MatchesAll(RuleContext ctx, SegmentEnumerator pos)
        {
            foreach (var seg in _match)
            {
                if (!seg.Matches(ctx, pos))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            uint count = 0;

            // We call pos.Mark() at the beginning and after every successful
            // match. At the end, if we are going to return true, we call
            // pos.Revert(). This is for the case where we have already matched
            // the minimum number of segments, and the next call to MatchesAll
            // returns false but nonetheless consumes some segments from the
            // enumeration. Those segments need to be put back for the next
            // matcher.

            pos.Mark();
            while (MatchesAll(ctx, pos))
            {
                pos.Mark();
                count++;
                if (_maxMatches.HasValue && _maxMatches <= count)
                {
                    break;
                }
            }

            var rv = count >= _minMatches;
            if (rv)
            {
                pos.Revert();
            }

            return rv;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            Matches(ctx, pos);
        }

        public bool IsMatchOnlySegment { get { return true; } }
        public string MatchString 
        { 
            get 
            { 
                var str = new StringBuilder();
                str.Append("(");
                str.Append(_match.Aggregate("", (t, s) => { return t + s.MatchString; }));
                str.Append(")");

                // add operators
                if (_minMatches == 0 && _maxMatches == null)
                {
                    str.Append("*");
                }
                else if (_minMatches == 1 && _maxMatches == null)
                {
                    str.Append("+");
                }

                return str.ToString();
            }
        }
        public string CombineString { get { return ""; } }
    }
}
