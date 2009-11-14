using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Phonix
{
    public interface IMatrixMatcher : IEnumerable<FeatureValueBase>
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public class MatrixMatcher : IMatrixMatcher
    {
        public static MatrixMatcher AlwaysMatches = new MatrixMatcher(new FeatureValueBase[] {});

        private readonly IEnumerable<FeatureValueBase> _values;

        public MatrixMatcher(FeatureMatrix fm)
        {
            List<FeatureValueBase> list = new List<FeatureValueBase>();
            var iter = fm.GetEnumerator(true);
            while (iter.MoveNext())
            {
                list.Add(iter.Current);
            }
            iter.Dispose();

            _values = list;
        }

        public MatrixMatcher(IEnumerable<FeatureValueBase> values)
        {
            _values = values;
        }

#region IEnumerable<FeatureValue> members

        public IEnumerator<FeatureValueBase> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            if (matrix == null)
            {
                return false;
            }

            foreach (FeatureValueBase fv in this)
            {
                FeatureValue compare;

                // If this is a variable value, then replace it with the real
                // value from the context. If the real value hasn't been set in
                // the context yet, then save the value of the current matrix
                // in the context.

                if (fv == fv.Feature.VariableValue)
                {
                    if (ctx == null)
                    {
                        throw new InvalidOperationException("context cannot be null for match with variables");
                    }
                    if (!ctx.VariableFeatures.ContainsKey(fv.Feature))
                    {
                        ctx.VariableFeatures[fv.Feature] = matrix[fv.Feature];
                    }
                    compare = ctx.VariableFeatures[fv.Feature];
                }
                else
                {
                    compare = fv as FeatureValue;
                }
                Debug.Assert(compare != null, "compare != null");

                if (matrix[fv.Feature] != compare)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
