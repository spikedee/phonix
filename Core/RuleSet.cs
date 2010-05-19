using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
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
        public event Action<Rule, IFeatureValue> UndefinedVariableUsed;
        public event Action<Rule, ScalarFeature, int> ScalarValueRangeViolation;
        public event Action<Rule, ScalarFeature, string> InvalidScalarValueOp;

        public RuleSet()
        {
            RuleDefined += (r) => {};
            RuleRedefined += (r1, r2) => {};
            RuleEntered += (r, w) => {};
            RuleApplied += (r, w, s) => {};
            RuleExited += (r, w) => {};
            UndefinedVariableUsed += (r, v) => {};
            ScalarValueRangeViolation += (r, f, v) => {};
            InvalidScalarValueOp += (r, f, s) => {};
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
            AddRuleEventHandlers(rule);

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
            AddRuleEventHandlers(rule);

            _persistent.Add(rule);
        }

        private void AddRuleEventHandlers(Rule rule)
        {
            rule.Entered += (r, w) => RuleEntered(r, w);
            rule.Exited += (r, w) => RuleExited(r, w);
            rule.Applied += (r, w, s) => RuleApplied(r, w, s);
            rule.UndefinedVariableUsed += (r, v) => UndefinedVariableUsed(r, v);
            rule.ScalarValueRangeViolation += (r, f, v) => ScalarValueRangeViolation(r, f, v);
            rule.InvalidScalarValueOp += (r, f, v) => InvalidScalarValueOp(r, f, v);
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
