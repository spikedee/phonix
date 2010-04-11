using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Phonix
{
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

            override public string ToString()
            {
                return "[]";
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

        override public string ToString()
        {
            var str = new StringBuilder();
            str.Append("[");
            str.Append(String.Join(" ", new List<ICombinable>(_values).ConvertAll(s => s.ToString()).ToArray()));
            str.Append("]");
            return str.ToString();
        }
    }
}
