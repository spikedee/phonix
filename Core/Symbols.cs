using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
    public class Symbol : IMatrixMatcher, IMatrixCombiner
    {
        public static readonly Symbol Unknown = new Symbol("[?]", FeatureMatrix.Empty);

        public readonly string Label;
        public readonly FeatureMatrix FeatureMatrix;

        public Symbol(string label, FeatureMatrix fm)
        {
            if (label == null || fm == null)
            {
                throw new ArgumentNullException();
            }

            Label = label;
            FeatureMatrix = fm;
        }

        public override string ToString()
        {
            return Label;
        }

        public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            return FeatureMatrix.Equals(matrix);
        }

        public FeatureMatrix Combine(RuleContext ctx, FeatureMatrix matrix)
        {
            return FeatureMatrix;
        }

#region IEnumerable members

        IEnumerator<IMatchable> IEnumerable<IMatchable>.GetEnumerator()
        {
            foreach (FeatureValue fv in this)
            {
                yield return fv;
            }
            yield break;
        }

        IEnumerator<ICombinable> IEnumerable<ICombinable>.GetEnumerator()
        {
            foreach (FeatureValue fv in this)
            {
                yield return fv;
            }
            yield break;
        }

        public IEnumerator<FeatureValue> GetEnumerator()
        {
            return FeatureMatrix.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

    }

    public class Diacritic : Symbol
    {
        // This is an empty subclass, used only to enforce the distinction
        // between Diacritics and Symbols where necessary
        
        public Diacritic(string label, FeatureMatrix fm)
            : base(label, fm)
        {
        }
    }

    public class CompositeSymbol : Symbol
    {
        public readonly Symbol BaseSymbol;
        public readonly IEnumerable<Diacritic> Diacritics;

        public CompositeSymbol(Symbol baseSymbol, params Diacritic[] diacritics)
            : base(CombineSymbols(baseSymbol, diacritics), CombineFeatures(baseSymbol, diacritics))
        {
            var compos = baseSymbol as CompositeSymbol;
            if (compos != null)
            {
                BaseSymbol = compos.BaseSymbol;
                var diaList = compos.Diacritics.ToList();
                diaList.AddRange(diacritics);
                Diacritics = diaList;
            }
            else
            {
                BaseSymbol = baseSymbol;
                Diacritics = diacritics;
            }
        }

        static private string CombineSymbols(Symbol baseSymbol, Diacritic[] diacritics)
        {
            var str = new StringBuilder();
            str.Append(baseSymbol.Label);
            foreach (var d in diacritics)
            {
                str.Append(d.Label);
            }
            return str.ToString();
        }

        static private FeatureMatrix CombineFeatures(Symbol baseSymbol, Diacritic[] diacritics)
        {
            var fm = baseSymbol.FeatureMatrix;
            foreach (var d in diacritics)
            {
                var combo = new MatrixCombiner(d.FeatureMatrix);
                fm = combo.Combine(null, fm);
            }
            return fm;
        }
    }
}
