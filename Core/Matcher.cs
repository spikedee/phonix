using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Phonix
{
    public interface IMatrixMatcher : IEnumerable<AbstractFeatureValue>
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public class MatrixMatcher : IMatrixMatcher
    {
        public static MatrixMatcher AlwaysMatches = new MatrixMatcher(new AbstractFeatureValue[] {});

        private readonly IEnumerable<AbstractFeatureValue> _values;

        public MatrixMatcher(FeatureMatrix fm)
        {
            List<AbstractFeatureValue> list = new List<AbstractFeatureValue>();
            var iter = fm.GetEnumerator(true);
            while (iter.MoveNext())
            {
                list.Add(iter.Current);
            }
            iter.Dispose();

            _values = list;
        }

        public MatrixMatcher(IEnumerable<AbstractFeatureValue> values)
        {
            var list = new List<AbstractFeatureValue>(values);
            _values = list;
        }

#region IEnumerable<AbstractFeatureValue> members

        public IEnumerator<AbstractFeatureValue> GetEnumerator()
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

            foreach (AbstractFeatureValue fvMatcher in this)
            {
                AbstractFeatureValue fvCompare;

                // If this is a variable value, then replace it with the real
                // value from the context. If the real value hasn't been set in
                // the context yet, then save the value of the current matrix
                // in the context. If it's a node value, then just check that
                // matrix is not null for that node.

                var fvMatrix = matrix[fvMatcher.Feature];
                if (fvMatcher == fvMatcher.Feature.VariableValue)
                {
                    if (ctx == null)
                    {
                        throw new InvalidOperationException("context cannot be null for match with variables");
                    }
                    if (!ctx.VariableFeatures.ContainsKey(fvMatcher.Feature))
                    {
                        ctx.VariableFeatures[fvMatcher.Feature] = fvMatrix;
                    }
                    fvCompare = ctx.VariableFeatures[fvMatcher.Feature];
                }
                else if (fvMatcher.Feature is NodeFeature && 
                         fvMatcher != fvMatcher.Feature.NullValue &&
                         fvMatrix != fvMatcher.Feature.NullValue)
                {
                    // We're looking for a non-null node, and the matrix has a
                    // node which isn't null. Assign fvMatrix to fvCompare, so
                    // that it will match below.
                    fvCompare = fvMatrix;
                }
                else
                {
                    fvCompare = fvMatcher;
                }

                if (fvMatrix != fvCompare)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
