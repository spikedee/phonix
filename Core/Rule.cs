using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
    public enum Direction
    {
        Rightward,
        Leftward
    }

    public sealed class Rule : AbstractRule
    {
        public Rule(string name, IEnumerable<IRuleSegment> segments, IEnumerable<IRuleSegment> excluded)
            : base(name)
        {
            if (segments == null || excluded == null)
            {
                throw new ArgumentNullException();
            }

            Segments = new List<IRuleSegment>(segments);
            ExcludedSegments = new List<IRuleSegment>(excluded);
            _hasExcluded = ExcludedSegments.Count() > 0;

            UndefinedVariableUsed += (r, v) => {};
            ScalarValueRangeViolation += (r, f, v) => {};
            InvalidScalarValueOp += (r, f, s) => {};
        }

        public readonly IEnumerable<IRuleSegment> Segments;
        public readonly IEnumerable<IRuleSegment> ExcludedSegments;
        private readonly bool _hasExcluded = false;
        private Random _random = new Random();

        public IMatrixMatcher Filter { get; set; }
        public Direction Direction { get; set; }

        public event Action<Rule, IFeatureValue> UndefinedVariableUsed;
        public event Action<Rule, ScalarFeature, int> ScalarValueRangeViolation;
        public event Action<Rule, ScalarFeature, string> InvalidScalarValueOp;

        private double _applicationRate = 1.0;
        public double ApplicationRate
        {
            get { return _applicationRate; }
            set 
            { 
                if (value < 0 || value > 1)
                {
                    throw new ArgumentException("ApplicationRate must be between zero and one");
                }
                _applicationRate = value;
            }
        }

        private string _description;
        public override string Description
        {
            get
            {
                if (_description == null)
                {

                    // build the description string. this is kinda complicated,
                    // which is why we don't do it until we're asked (and we
                    // save the result)
                    
                    var leftCtx = new StringBuilder();
                    var rightCtx = new StringBuilder();
                    var leftAct = new StringBuilder();
                    var rightAct = new StringBuilder();
                    bool leftSide = true;

                    // iterate over all of the segments here, and append their
                    // strings to the left or right context or left/right action as
                    // necessary. We start by putting match segments on the left
                    // context, then switch to the right context as soon as we've
                    // encountered any action segments.
                    
                    foreach (var seg in Segments)
                    {
                        if (seg.IsMatchOnlySegment)
                        {
                            if (leftSide)
                            {
                                leftCtx.Append(seg.MatchString);
                            }
                            else
                            {
                                rightCtx.Append(seg.MatchString);
                            }
                        }
                        else
                        {
                            leftAct.Append(seg.MatchString);
                            rightAct.Append(seg.CombineString);
                            leftSide = false;
                        }
                    }

                    _description = String.Format("{0} => {1} / {2} _ {3}", 
                            leftAct.ToString(), 
                            rightAct.ToString(), 
                            leftCtx.ToString(), 
                            rightCtx.ToString());
                }
                return _description;
            }
        }

        public override void Apply(Word word)
        {
            OnEntered(word);
            try
            {
                foreach (var slice in AppliedEnumeration(word))
                {
                    OnApplied(word, slice);
                }
            }
            finally
            {
                OnExited(word);
            }
        }

        internal IEnumerable<IWordSlice> AppliedEnumeration(Word word)
        {
            foreach (var slice in word.Slice(Direction, Filter))
            {
                if (_applicationRate < 1.0)
                {
                    if (_random.NextDouble() > _applicationRate)
                    {
                        // skip this potential application of the rule
                        continue;
                    }
                }

                var ctx = new RuleContext();

                // match all of the segments
                if (!MatchesSegments(slice, Segments, ctx))
                {
                    continue;
                }

                // ensure that we don't match the excluded segments
                if (_hasExcluded && MatchesSegments(slice, ExcludedSegments, ctx))
                {
                    continue;
                }

                // apply all of the segments
                using (var mutableSegment = slice.GetMutableEnumerator())
                {
                    foreach (var ruleSegment in Segments)
                    {
                        try
                        {
                            ruleSegment.Combine(ctx, mutableSegment);
                        }
                        catch (UndefinedFeatureVariableException ex)
                        {
                            UndefinedVariableUsed(this, ex.Variable);
                        }
                        catch (ScalarValueRangeException ex)
                        {
                            ScalarValueRangeViolation(this, ex.Feature, ex.Value);
                        }
                        catch (InvalidScalarOpException ex)
                        {
                            InvalidScalarValueOp(this, ex.Feature, ex.Message);
                        }
                    }
                    yield return slice;
                }
            }
            yield break;
        }

        private static bool MatchesSegments(IWordSlice slice, IEnumerable<IRuleSegment> segments, RuleContext ctx)
        {
            using (var seg = slice.GetEnumerator())
            {
                foreach (var ruleSeg in segments)
                {
                    if (!ruleSeg.Matches(ctx, seg))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override string ShowApplication(Word word, IWordSlice slice, SymbolSet symbolSet)
        {
            FeatureMatrix current = null;
            Segment firstSliceSeg = null;
            try
            {
                var ctx = new RuleContext();
                var seg = Segments.GetEnumerator();
                var pos = slice.GetEnumerator();

                // get the first segment in the slice
                pos.MoveNext();
                firstSliceSeg = pos.Current;
                pos = slice.GetEnumerator(); // reset the pos

                // match until we get to the current segment, so that we can
                // display which segment was acted upon
                while (seg.MoveNext() && seg.Current.IsMatchOnlySegment)
                {
                    // we call Matches() but throw away the value, since we're
                    // only calling this in order to move the enumerator
                    seg.Current.Matches(ctx, pos);
                }
                current = pos.MoveNext() ? pos.Current.Matrix : null;
            }
            catch (SegmentDeletedException)
            {
                // this occurs when we try to get the enumerator for a deleted
                // segment. this exception (and only this exception) can be
                // safely swallowed
            }

            var str = new StringBuilder();
            bool inSlice = false;
            foreach (var seg in word)
            {
                string marker = " ";
                Symbol symbol;
                if (seg == firstSliceSeg)
                {
                    inSlice = true;
                }

                if (current != null && inSlice && seg.Matrix == current)
                {
                    marker = ">";
                }

                try
                {
                    symbol = symbolSet.Spell(seg.Matrix);
                }
                catch (SpellingException)
                {
                    symbol = Symbol.Unknown;
                }

                str.AppendLine(String.Format("{0} {1} : {2}", marker, symbol, seg.Matrix));
            }

            return str.ToString();
        }
    }
}
