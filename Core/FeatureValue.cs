using System.Collections.Generic;

namespace Phonix
{
    public abstract class FeatureValue : AbstractFeatureValue, IMatchCombine
    {
        protected FeatureValue(Feature feature, string desc)
            : base(feature, desc)
        {
        }

        new public Feature Feature
        {
            get
            {
                return base.Feature;
            }
        }

        virtual public bool Matches(RuleContext ctx, Segment segment)
        {
            return this == segment.Matrix[this.Feature];
        }

        virtual public IEnumerable<FeatureValue> CombineValues(RuleContext ctx, MutableSegment segment)
        {
            return new FeatureValue[] { this };
        }
    }
}
