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
        bool Matches(RuleContext ctx, SegmentEnumerator segment);

        /* This function consumes zero or more segments from the enumeration,
         * modifying them as appropriate. The directions about MoveNext()
         * mentioned for Matches also applies here.
         */
         void Combine(RuleContext ctx, MutableSegmentEnumerator segment); 
    
    }

    public class RuleContext
    {
        public readonly Dictionary<Feature, FeatureValue> VariableFeatures 
            = new Dictionary<Feature, FeatureValue>();
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

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Current = _combo.Combine(pos.Current);
        }
    }

    public class VariableMatrixSegment : IRuleSegment
    {
        private readonly FeatureMatrixSegment _matrix;
        private readonly IEnumerable<Feature> _matchVariables;
        private readonly IEnumerable<Feature> _comboVariables;

        public VariableMatrixSegment(
                FeatureMatrixSegment fmSeg, 
                IEnumerable<Feature> matchVariables, 
                IEnumerable<Feature> comboVariables)
        {
            if (fmSeg == null || matchVariables == null || comboVariables == null)
            {
                throw new ArgumentNullException();
            }

            _matrix = fmSeg;
            _matchVariables = matchVariables;
            _comboVariables = comboVariables;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (_matrix.Matches(ctx, pos))
            {

                // for variable features, if the variable has already been
                // defined in this context, match against that value. If the
                // variable hasn't been defined, save this value in the current
                // context.

                foreach (var feature in _matchVariables)
                {
                    if (ctx.VariableFeatures.ContainsKey(feature))
                    {
                        if (pos.Current[feature] != ctx.VariableFeatures[feature])
                        {
                            return false;
                        }
                    }
                    else 
                    {
                        ctx.VariableFeatures[feature] = pos.Current[feature];
                    }
                }
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            _matrix.Combine(ctx, pos);

            // when combining variable features, every feature referenced
            // should already have been defined in a match.

            var variableVals = new List<FeatureValue>();
            foreach (var feature in _comboVariables)
            {
                if (ctx.VariableFeatures.ContainsKey(feature))
                {
                    variableVals.Add(ctx.VariableFeatures[feature]);
                }
                else
                {
                    throw new InvalidOperationException("variable feature value refers to undefined variable");
                }
            }
            pos.Current = new MatrixCombiner(new FeatureMatrix(variableVals)).Combine(pos.Current);
        }
    }

    public class DeletingSegment : IRuleSegment
    {
        private IMatrixMatcher _match;

        public DeletingSegment(IMatrixMatcher match)
        {
            _match = match;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
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

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            // always return true, but take nothing from the input list
            return true;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
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
                var context = new RuleContext();
                var matrix = slice.Current.GetEnumerator();
                bool matchedAll = true;
                foreach (var segment in Segments)
                {
                    if (!segment.Matches(context, matrix))
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
                    segment.Combine(context, wordSegment);
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
