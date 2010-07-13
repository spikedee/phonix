using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public class RuleSet
    {
        private List<AbstractRule> _persistent = new List<AbstractRule>();
        public IEnumerable<AbstractRule> PersistentRules
        { 
            get { return _persistent; }
        }

        private List<AbstractRule> _ordered = new List<AbstractRule>();
        public IEnumerable<AbstractRule> OrderedRules
        { 
            get { return _ordered; }
        }

        public event Action<AbstractRule> RuleDefined = r => {};
        public event Action<AbstractRule, AbstractRule> RuleRedefined = (r1, r2) => {};

        public event Action<Rule, IFeatureValue> UndefinedVariableUsed = (r, v) => {};
        public event Action<Rule, ScalarFeature, int> ScalarValueRangeViolation = (r, f, v) => {};
        public event Action<Rule, ScalarFeature, string> InvalidScalarValueOp = (r, f, s) => {};

        // for performance reasons, the following events have to be checked
        // against null at every invocation (which is actually faster than
        // executing a nop delegate). They all have an OnX() method that safely
        // wraps the invocation.
        public event Action<AbstractRule, Word> RuleEntered;
        public event Action<AbstractRule, Word, IWordSlice> RuleApplied;
        public event Action<AbstractRule, Word> RuleExited;

        public void Add(AbstractRule rule)
        {
            RuleDefined(rule);
            foreach (AbstractRule dup in OrderedRules.Where(existing => existing.Name.Equals(rule.Name)))
            {
                RuleRedefined(dup, rule);
            }

            if (rule is Rule)
            {
                AddRuleEventHandlers((Rule) rule);
            }

            _ordered.Add(rule);
        }

        public void AddPersistent(AbstractRule rule)
        {
            RuleDefined(rule);
            foreach (AbstractRule dup in OrderedRules.Where(existing => existing.Name.Equals(rule.Name)))
            {
                RuleRedefined(dup, rule);
            }
            if (rule is Rule)
            {
                AddRuleEventHandlers((Rule) rule);
            }

            _persistent.Add(rule);
        }

        private void AddRuleEventHandlers(Rule rule)
        {
            rule.UndefinedVariableUsed += (r, v) => UndefinedVariableUsed(r, v);
            rule.ScalarValueRangeViolation += (r, f, v) => ScalarValueRangeViolation(r, f, v);
            rule.InvalidScalarValueOp += (r, f, v) => InvalidScalarValueOp(r, f, v);
        }

        private void OnRuleEntered(AbstractRule rule, Word word)
        {
            var entered = RuleEntered;
            if (entered != null)
            {
                entered(rule, word);
            }
        }

        private void OnRuleExited(AbstractRule rule, Word word)
        {
            var exited = RuleExited;
            if (exited != null)
            {
                exited(rule, word);
            }
        }

        private void OnRuleApplied(AbstractRule rule, Word word, IWordSlice slice)
        {
            var applied = RuleApplied;
            if (applied != null)
            {
                applied(rule, word, slice);
            }
        }

        public void ApplyAll(Word word)
        {
            // apply persistent rules once at the beginning of execution
            ApplyPersistentRules(word);

            foreach (var rule in OrderedRules)
            {
                Action<AbstractRule, Word, IWordSlice> applyPersistentRules = (innerRule, innerWord, slice) => 
                {
                    ApplyPersistentRules(innerWord);
                };
                if (PersistentRules.Count() > 0)
                {
                    rule.Applied += applyPersistentRules;
                }

                try
                {
                    ApplyRule(rule, word);
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
                ApplyRule(rule, word);
            }
        }

        private void ApplyRule(AbstractRule rule, Word word)
        {
            try
            {
                if (RuleEntered != null)
                {
                    rule.Entered += OnRuleEntered;
                }
                if (RuleApplied != null)
                {
                    rule.Applied += OnRuleApplied;
                }
                if (RuleExited != null)
                {
                    rule.Exited += OnRuleExited;
                }

                rule.Apply(word);
            }
            finally
            {
                rule.Entered -= OnRuleEntered;
                rule.Applied -= OnRuleApplied;
                rule.Exited -= OnRuleExited;
            }
        }
    }
}
