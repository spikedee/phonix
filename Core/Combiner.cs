using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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

    public class MatrixCombiner : IMatrixCombiner
    {
        private class NullCombinator : IMatrixCombiner
        {
            public IEnumerator<ICombinable> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public FeatureMatrix Combine(RuleContext ctx, FeatureMatrix matrix)
            {
                return matrix;
            }
        }

        public static IMatrixCombiner NullCombiner = new NullCombinator();

        private readonly IEnumerable<ICombinable> _values;

        public MatrixCombiner(FeatureMatrix fm)
        {
            var list = new List<ICombinable>();
            var iter = fm.GetEnumerator(true);
            while (iter.MoveNext())
            {
                list.Add(iter.Current);
            }
            iter.Dispose();

            _values = list;
        }

        public MatrixCombiner(IEnumerable<ICombinable> values)
        {
            _values = values;
        }

        public MatrixCombiner(IEnumerable<IMatchCombine> values)
            : this(new List<IMatchCombine>(values).ConvertAll<ICombinable>(v => v))
        {
        }


#region IEnumerable<ICombinable> members

        public IEnumerator<ICombinable> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public FeatureMatrix Combine(RuleContext ctx, FeatureMatrix matrix)
        {
            var comboValues = new List<FeatureValue>();
            comboValues.AddRange(matrix);

            foreach (var combo in this)
            {
                comboValues.AddRange(combo.GetValues(ctx, matrix));
            }

            return new FeatureMatrix(comboValues);
        }
    }

    internal class DelegateCombiner : ICombinable
    {
        public delegate IEnumerable<FeatureValue> ComboFunc(RuleContext ctx, FeatureMatrix fm);
        private readonly ComboFunc _combo;

        public DelegateCombiner(ComboFunc combo)
        {
            _combo = combo;
        }

        public IEnumerable<FeatureValue> GetValues(RuleContext ctx, FeatureMatrix matrix)
        {
            return _combo(ctx, matrix);
        }
    }
}
