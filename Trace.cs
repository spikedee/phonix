using System;

#pragma warning disable 414

namespace Phonix
{
    public static class Trace
    {
        private class NullTracer
        {
            public NullTracer()
            {
                // add null events to all of our public events, avoiding the
                // hassle of null checks in our implementations.

                OnFeatureDefined += (f) => {};
                OnFeatureRedefined += (f1, f2) => {};

                OnSymbolDefined += (s) => {};
                OnSymbolRedefined += (s1, s2) => {};
                OnSymbolDuplicate += (s1, s2) => {};

                OnRuleDefined += (r) => {};
                OnRuleRedefined += (r1, r2) => {};

                OnRuleEntered += (r, w) => {};
                OnRuleApplied += (r, w, s) => {};
                OnRuleExited += (r, w) => {};
            }
        }

        private static NullTracer tracer = new NullTracer();

        public static event Action<Feature> OnFeatureDefined;
        public static event Action<Feature, Feature> OnFeatureRedefined;

        public static event Action<Symbol> OnSymbolDefined;
        public static event Action<Symbol, Symbol> OnSymbolRedefined;
        public static event Action<Symbol, Symbol> OnSymbolDuplicate;

        public static event Action<Rule> OnRuleDefined;
        public static event Action<Rule, Rule> OnRuleRedefined;

        public static event Action<Rule, Word> OnRuleEntered;
        public static event Action<Rule, Word, IWordSlice> OnRuleApplied;
        public static event Action<Rule, Word> OnRuleExited;

        public static void FeatureDefined(Feature f)
        {
            OnFeatureDefined(f);
        }

        public static void FeatureRedefined(Feature oldFeature, Feature newFeature)
        {
            OnFeatureRedefined(oldFeature, newFeature);
        }

        public static void SymbolDefined(Symbol s)
        {
            OnSymbolDefined(s);
        }

        public static void SymbolRedefined(Symbol oldSymbol, Symbol newSymbol)
        {
            OnSymbolRedefined(oldSymbol, newSymbol);
        }

        public static void SymbolDuplicate(Symbol a, Symbol b)
        {
            OnSymbolDuplicate(a, b);
        }

        public static void RuleDefined(Rule r)
        {
            OnRuleDefined(r);
        }

        public static void RuleRedefined(Rule oldRule, Rule newRule)
        {
            OnRuleRedefined(oldRule, newRule);
        }

        public static void RuleEntered(Rule r, Word w)
        {
            OnRuleEntered(r, w);
        }

        public static void RuleApplied(Rule r, Word w, IWordSlice slice)
        {
            OnRuleApplied(r, w, slice);
        }

        public static void RuleExited(Rule r, Word w)
        {
            OnRuleExited(r, w);
        }
    }
}
