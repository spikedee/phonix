using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Phonix
{
    public interface IMatrixMatcher : IEnumerable<IMatchable>
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public interface IMatchable
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public interface IMatchCombine : IMatchable, ICombinable
    {
        // this exists just to provide a convenient way to specify both
        // behaviors
    }

    public class MatrixMatcher : IMatrixMatcher
    {
        public static MatrixMatcher AlwaysMatches = new MatrixMatcher(new IMatchable[] {});

        private readonly IEnumerable<IMatchable> _values;

        public MatrixMatcher(FeatureMatrix fm)
        {
            var list = new List<IMatchable>();
            var iter = fm.GetEnumerator(true);
            while (iter.MoveNext())
            {
                list.Add(iter.Current);
            }
            iter.Dispose();

            _values = list;
        }

        public MatrixMatcher(IEnumerable<IMatchable> values)
        {
            var list = new List<IMatchable>(values);
            _values = list;
        }

        public MatrixMatcher(IEnumerable<IMatchCombine> values)
            : this(new List<IMatchCombine>(values).ConvertAll<IMatchable>(v => v))
        {
        }

#region IEnumerable<AbstractFeatureValue> members

        public IEnumerator<IMatchable> GetEnumerator()
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

            foreach (IMatchable match in this)
            {
                if (!match.Matches(ctx, matrix))
                {
                    return false;
                }
            }
            return true;
        }
#if false

                AbstractFeatureValue fvCompare;

                // If this is a variable value, then replace it with the real
                // value from the context. If the real value hasn't been set in
                // the context yet, then save the value of the current matrix
                // in the context. If it's a node value, then just check that
                // matrix is not null for that node.

                var fvMatrix = matrix[fvMatcher.Feature];
                if (fvMatcher == fvMatcher.Feature.VariableValue)
                {
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
            return true;
#endif
    }
}
