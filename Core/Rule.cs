using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public class RuleContext
    {
        private Dictionary<Feature, FeatureValue> _variableFeatures;
        private Dictionary<NodeFeature, IEnumerable<FeatureValue>> _variableNodes;

        public Dictionary<Feature, FeatureValue> VariableFeatures
        {
            get
            {
                if (_variableFeatures == null)
                {
                    _variableFeatures = new Dictionary<Feature, FeatureValue>();
                }
                return _variableFeatures;
            }
        }

        public Dictionary<NodeFeature, IEnumerable<FeatureValue>> VariableNodes
        {
            get
            {
                if (_variableNodes == null)
                {
                    _variableNodes = new Dictionary<NodeFeature, IEnumerable<FeatureValue>>();
                }
                return _variableNodes;
            }
        }
    }

    public class Rule
    {
        public readonly string Name;

        public readonly IEnumerable<IRuleSegment> Segments;

        public readonly IEnumerable<IRuleSegment> ExcludedSegments;

        public IMatrixMatcher Filter { get; set; }

        public Direction Direction { get; set; }

        // Application rate should vary from 0 to 1000
        private int _applicationRate = 1000;
        public double ApplicationRate
        {
            get { return ((double)_applicationRate) / 1000; }
            set { 
                if (value < 0 || value > 1)
                {
                    throw new ArgumentException("ApplicationRate must be between zero and one");
                }
                _applicationRate = (int)(value * 1000);
            }
        }

        private Random _random = new Random();

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
        }

        public void Apply(Word word)
        {
            Entered(this, word);

            try
            {
                var slice = word.GetSliceEnumerator(Direction, Filter);

                while (slice.MoveNext())
                {
                    if (_applicationRate < 1000)
                    {
                        if (_random.Next(0, 1000) > _applicationRate)
                        {
                            // skip this potential application of the rule
                            continue;
                        }
                    }

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
            }
            finally
            {
                Exited(this, word);
            }
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
