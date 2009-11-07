using System;
using System.Collections.Generic;

namespace Phonix
{
    public interface IRuleSegment
    {

        /* This function should return true if this matches the segment(s) at
         * the next position(s) of the enumeration. When this is called, the
         * iterator will be at the start of the enumeration or on the last
         * segment matched by the previous IRuleSegment, so implementations
         * should call segment.MoveNext() zero or more times (depending on how
         * many segments from the input they consume) _before_ testing
         * segment.Current.
         */
        bool Matches(SegmentEnumerator segment);

        /* This function consumes zero or more segments from the enumeration,
         * modifying them as appropriate. The directions about MoveNext()
         * mentioned for Matches also applies here.
         */
         void Combine(MutableSegmentEnumerator segment); 
    
    }

    public class FeatureMatrixSegment : IRuleSegment
    {
        private IMatrixMatcher _match;
        private IMatrixCombiner _combo;

        public FeatureMatrixSegment(IMatrixMatcher match, IMatrixCombiner combo)
        {
            _match = match;
            _combo = combo;
        }

        public bool Matches(SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Current = _combo.Combine(pos.Current);
        }
    }

    public class DeletingSegment : IRuleSegment
    {
        private IMatrixMatcher _match;

        public DeletingSegment(IMatrixMatcher match)
        {
            _match = match;
        }

        public bool Matches(SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Delete();
        }
    }
        
    public class InsertingSegment : IRuleSegment
    {
        private FeatureMatrix _insert;

        public InsertingSegment(IMatrixCombiner insert)
        {
            _insert = insert.Combine(FeatureMatrix.Empty);
        }

        public bool Matches(SegmentEnumerator pos)
        {
            // always return true, but take nothing from the input list
            return true;
        }

        public void Combine(MutableSegmentEnumerator pos)
        {
            pos.InsertAfter(_insert);
            pos.MoveNext();
        }
    }

    public class Rule
    {
        public readonly string Name;

        public readonly IEnumerable<IRuleSegment> Segments;

        public IMatrixMatcher Filter { get; set; }

        public Rule(string name, IEnumerable<IRuleSegment> segments)
        {
            if (name == null || segments == null)
            {
                throw new ArgumentNullException();
            }

            Name = name;
            Segments = segments;
        }

        public override string ToString()
        {
            // TODO
            return Name;
            //return String.Format("{0}: {1} => {2}", Name, Condition, Action);
        }

        public void Apply(Word word)
        {
            Trace.RuleEntered(this, word);

            var slice = word.GetSliceEnumerator(Direction.Rightward, Filter);

            while (slice.MoveNext())
            {
                // match all of the segments
                var matrix = slice.Current.GetEnumerator();
                bool matchedAll = true;
                foreach (var segment in Segments)
                {
                    if (!segment.Matches(matrix))
                    {
                        matchedAll = false;
                        break;
                    }
                }
                matrix.Dispose();

                if (!matchedAll)
                {
                    continue;
                }

                // apply all of the segments
                var wordSegment = slice.Current.GetMutableEnumerator();
                foreach (var segment in Segments)
                {
                    segment.Combine(wordSegment);
                }
                wordSegment.Dispose();
                Trace.RuleApplied(this, word, slice.Current);
            }

            Trace.RuleExited(this, word);
        }

    }

    public class RuleSet : List<Rule>
    {
        new public void Add(Rule r)
        {
            Trace.RuleDefined(r);
            foreach (Rule existing in this)
            {
                if (existing.Name.Equals(r.Name))
                {
                    Trace.RuleRedefined(existing, r);
                }
            }

            base.Add(r);
        }

        public void ApplyAll(Word word)
        {
            foreach (var rule in this)
            {
                rule.Apply(word);
            }
        }
    }

}
