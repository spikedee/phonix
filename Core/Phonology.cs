using System;

namespace Phonix
{
    public class Phonology
    {
        public readonly FeatureSet FeatureSet;
        public readonly SymbolSet SymbolSet;
        public readonly RuleSet RuleSet;

        internal Shell.Config Config;

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
    }
}
