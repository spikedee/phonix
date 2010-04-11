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
        FeatureMatrix Combine(RuleContext ctx, FeatureMatrix matrix);
    }

    public interface ICombinable
    {
        IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix);
    }
}
