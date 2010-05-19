using System.Collections.Generic;

namespace Phonix
{
    public interface IFeatureValue : IMatchable, ICombinable
    {
        // this method can throw a NotImplementedException -- caller beware
        FeatureValue ToFeatureValue();
    }

    public abstract class FeatureValue : AbstractFeatureValue, IFeatureValue
    {
        private readonly IEnumerable<FeatureValue> _thisEnumerable;

        protected FeatureValue(Feature feature, string desc)
            : base(feature, desc)
        {
            _thisEnumerable = new FeatureValue[] { this };
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
            return _thisEnumerable;
        }

        virtual public FeatureValue ToFeatureValue()
        {
            return this;
        }
    }
}
