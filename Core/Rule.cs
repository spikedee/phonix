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

        public event Action<Rule, Word> Entered;
        public event Action<Rule, Word, IWordSlice> Applied;
        public event Action<Rule, Word> Exited;
        public event Action<Rule, IMatchCombine> UndefinedVariableUsed;

        public Rule(string name, IEnumerable<IRuleSegment> segments, IEnumerable<IRuleSegment> excluded)
        {
            if (name == null || segments == null || excluded == null)
            {
                throw new ArgumentNullException();
            }

            Name = name;
            Segments = segments;
            ExcludedSegments = excluded;

            // add empty event handlers
            Entered += (r, w) => {};
            Applied += (r, w, s) => {};
            Exited += (r, w) => {};
            UndefinedVariableUsed += (r, v) => {};
        }

        public override string ToString()
        {
            // TODO
            return Name;
            //return String.Format("{0}: {1} => {2}", Name, Condition, Action);
        }

        public void Apply(Word word)
        {
            Entered(this, word);

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
                    try
                    {
                        segment.Combine(context, wordSegment);
                    }
                    catch (UndefinedFeatureVariableException ex)
                    {
                        UndefinedVariableUsed(this, ex.Variable);
                    }
                }
                wordSegment.Dispose();
                Applied(this, word, slice.Current);
            }

            Exited(this, word);
        }

    }

    public class RuleSet
    {
        private List<Rule> _persistent = new List<Rule>();
        public IEnumerable<Rule> PersistentRules
        { 
            get { return _persistent; }
        }

        private List<Rule> _ordered = new List<Rule>();
        public IEnumerable<Rule> OrderedRules
        { 
            get { return _ordered; }
        }

        public event Action<Rule> RuleDefined;
        public event Action<Rule, Rule> RuleRedefined;
        public event Action<Rule, Word> RuleEntered;
        public event Action<Rule, Word, IWordSlice> RuleApplied;
        public event Action<Rule, Word> RuleExited;
        public event Action<Rule, IMatchCombine> UndefinedVariableUsed;

        public RuleSet()
        {
            RuleDefined += (r) => {};
            RuleRedefined += (r1, r2) => {};
            RuleEntered += (r, w) => {};
            RuleApplied += (r, w, s) => {};
            RuleExited += (r, w) => {};
            UndefinedVariableUsed += (r, v) => {};
        }

        public void Add(Rule rule)
        {
            RuleDefined(rule);
            foreach (Rule existing in OrderedRules)
            {
                if (existing.Name.Equals(rule.Name))
                {
                    RuleRedefined(existing, rule);
                }
            }

            rule.Entered += (r, w) => RuleEntered(r, w);
            rule.Exited += (r, w) => RuleExited(r, w);
            rule.Applied += (r, w, s) => RuleApplied(r, w, s);
            rule.UndefinedVariableUsed += (r, v) => UndefinedVariableUsed(r, v);

            _ordered.Add(rule);
        }

        public void AddPersistent(Rule rule)
        {
            RuleDefined(rule);
            foreach (Rule existing in PersistentRules)
            {
                if (existing.Name.Equals(rule.Name))
                {
                    RuleRedefined(existing, rule);
                }
            }

            rule.Entered += (r, w) => RuleEntered(r, w);
            rule.Exited += (r, w) => RuleExited(r, w);
            rule.Applied += (r, w, s) => RuleApplied(r, w, s);
            rule.UndefinedVariableUsed += (r, v) => UndefinedVariableUsed(r, v);

            _persistent.Add(rule);
        }

        public void ApplyAll(Word word)
        {
            // apply persistent rules once at the beginning of execution
            ApplyPersistentRules(word);

            foreach (var rule in OrderedRules)
            {
                Action<Rule, Word, IWordSlice> applyPersistentRules = (innerRule, innerWord, slice) => 
                {
                    ApplyPersistentRules(innerWord); 
                };
                rule.Applied += applyPersistentRules;

                try
                {
                    rule.Apply(word);
                }
                finally
                {
                    rule.Applied -= applyPersistentRules;
                }
            }
        }

        private void ApplyPersistentRules(Word word)
        {
            foreach (var rule in PersistentRules)
            {
                rule.Apply(word);
            }
        }

    }

}
