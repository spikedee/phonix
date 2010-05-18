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

        public IEnumerable<FeatureValue> CombineValues(RuleContext ctx, MutableSegment segment)
        {
            return _combo(ctx, segment.Matrix);
        }
        
        override public string ToString()
        {
            return _desc;
        }
    }
}
