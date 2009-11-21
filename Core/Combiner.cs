using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Phonix
{
    public interface IMatrixCombiner : IEnumerable<AbstractFeatureValue>
    {
        FeatureMatrix Combine(RuleContext ctx, FeatureMatrix matrix);
    }

    public class MatrixCombiner : IMatrixCombiner
    {
        public static MatrixCombiner NullCombiner = new MatrixCombiner(FeatureMatrix.Empty);

        private readonly IEnumerable<AbstractFeatureValue> _values;

        public MatrixCombiner(FeatureMatrix fm)
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

        public MatrixCombiner(IEnumerable<AbstractFeatureValue> values)
        {
            _values = values;
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

        public FeatureMatrix Combine(RuleContext ctx, FeatureMatrix matrix)
        {
            if (this == NullCombiner)
            {
                return matrix;
            }

            var comboValues = new List<FeatureValue>();
            comboValues.AddRange(matrix);

            foreach (var fv in this)
            {

                // get variable values from the context. other values are added
                // directly to the list which creates the new matrix.

                if (fv == fv.Feature.VariableValue)
                {
                    if (ctx == null)
                    {
                        throw new InvalidOperationException("context cannot be null for combine with variables");
                    }

                    if (ctx.VariableFeatures.ContainsKey(fv.Feature))
                    {
                        comboValues.AddRange(ctx.VariableFeatures[fv.Feature].ToValueList());
                    }
                    else
                    {
                        // the user tried to set a variable that hasn't been
                        // defined. Warn them and leave the variable alone.
                        Trace.UndefinedVariableUsed(fv);
                    }
                }
                else
                {
                    Debug.Assert(fv is FeatureValue, "fv is FeatureValue");
                    comboValues.Add(fv as FeatureValue);
                }
            }

            return new FeatureMatrix(comboValues);
        }

    }
}
