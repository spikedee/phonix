using System.Collections.Generic;

namespace Phonix
{
    public interface IMatrixCombiner : IEnumerable<ICombinable>
    {
        void Combine(RuleContext ctx, MutableSegment segment);
    }

    public interface ICombinable
    {
        IEnumerable<FeatureValue> CombineValues(RuleContext ctx, MutableSegment segment);
    }
}
