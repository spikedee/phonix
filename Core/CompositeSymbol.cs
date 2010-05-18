using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
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
            var values = new List<FeatureValue>();
            values.AddRange(baseSymbol.FeatureMatrix);
            foreach (var d in diacritics)
            {
                values.AddRange(d.FeatureMatrix);
            }
            return new FeatureMatrix(values);
        }
    }
}
