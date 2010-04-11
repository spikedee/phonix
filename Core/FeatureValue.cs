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

        virtual public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            return this == matrix[this.Feature];
        }

        virtual public IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix)
        {
            return new FeatureValue[] { this };
        }
    }
}
