using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
