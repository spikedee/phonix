using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public class RuleContext
    {
        public readonly Dictionary<Feature, FeatureValue> VariableFeatures 
            = new Dictionary<Feature, FeatureValue>();

        public readonly Dictionary<NodeFeature, IEnumerable<FeatureValue>> VariableNodes
            = new Dictionary<NodeFeature, IEnumerable<FeatureValue>>();
    }

    public class Rule
    {
        public readonly string Name;

        public readonly IEnumerable<IRuleSegment> Segments;

        public readonly IEnumerable<IRuleSegment> ExcludedSegments;

        public IMatrixMatcher Filter { get; set; }

        public Direction Direction { get; set; }

        public Rule(string name, IEnumerable<IRuleSegment> segments, IEnumerable<IRuleSegment> excluded)
        {
            if (name == null || segments == null || excluded == null)
            {
                throw new ArgumentNullException();
            }

            Name = name;
            Segments = segments;
            ExcludedSegments = excluded;
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

            var slice = word.GetSliceEnumerator(Direction, Filter);

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

                // ensure that we don't match the excluded segments
                matrix = slice.Current.GetEnumerator();
                bool matchedExcluded = true;
                foreach (var segment in ExcludedSegments)
                {
                    if (!segment.Matches(context, matrix))
                    {
                        matchedExcluded = false;
                        break;
                    }
                }
                matrix.Dispose();

                if (matchedExcluded)
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
