using System;

namespace Phonix
{
    public class Phonology
    {
        public readonly FeatureSet FeatureSet;
        public readonly SymbolSet SymbolSet;
        public readonly RuleSet RuleSet;

        public PhonixConfig Config;

        public Phonology()
            : this(new FeatureSet(), new SymbolSet(), new RuleSet())
        {
        }

        public Phonology(FeatureSet fs, SymbolSet ss, RuleSet rs)
        {
            FeatureSet = fs;
            SymbolSet = ss;
            RuleSet = rs;
        }

        public void Merge(Phonology other)
        {
            foreach (Feature f in other.FeatureSet)
            {
                FeatureSet.Add(f);
            }

            foreach (var s in other.SymbolSet)
            {
                SymbolSet.Add(s.Key, s.Value);
            }

            foreach (Rule r in other.RuleSet)
            {
                RuleSet.Add(r);
            }
        }
    }
}
