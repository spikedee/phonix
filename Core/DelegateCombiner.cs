using System.Collections.Generic;

namespace Phonix
{
    internal class DelegateCombiner : ICombinable
    {
        public delegate IEnumerable<FeatureValue> ComboFunc(RuleContext ctx, FeatureMatrix fm);
        private readonly ComboFunc _combo;
        private readonly string _desc;

        public DelegateCombiner(ComboFunc combo, string desc)
        {
            _combo = combo;
            _desc = desc;
        }

        public IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix)
        {
            return _combo(ctx, matrix);
        }
        
        override public string ToString()
        {
            return _desc;
        }
    }
}
