using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
    public class Symbol : IMatrixMatcher, IMatrixCombiner
    {
        public readonly string Label;
        public readonly FeatureMatrix FeatureMatrix;

        private readonly MatrixMatcher _matcher;

        public Symbol(string label, FeatureMatrix fm)
        {
            if (label == null || fm == null)
            {
                throw new ArgumentNullException();
            }

            Label = label;
            FeatureMatrix = fm;
            _matcher = new MatrixMatcher(fm);
        }

        public override string ToString()
        {
            return Label;
        }

        public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            return _matcher.Matches(ctx, matrix);
        }

        public FeatureMatrix Combine(RuleContext ctx, FeatureMatrix matrix)
        {
            return FeatureMatrix;
        }

#region IEnumerable(AbstractFeatureValue) members

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

    public class SymbolSet : Dictionary<string, Symbol>
    {
        public void Add(Symbol s)
        {
            if (s == null)
            {
                throw new ArgumentNullException();
            }

            Trace.SymbolDefined(s);

            if (this.ContainsKey(s.Label))
            {
                Trace.SymbolRedefined(this[s.Label], s);
            }

            foreach (var existing in Values)
            {
                if (existing.FeatureMatrix.Equals(s.FeatureMatrix))
                {
                    Trace.SymbolDuplicate(existing, s);
                }
            }

            this[s.Label] = s;
        }

        public Symbol Spell(FeatureMatrix matrix)
        {
            int hiweight = 0;
            Symbol match = null;

            foreach (var s in this.Values)
            {
                if (s.Matches(null, matrix) && s.FeatureMatrix.Weight >= hiweight)
                {
                    hiweight = s.FeatureMatrix.Weight;
                    match = s;
                }
            }

            if (match == null)
            {
                throw new SpellingException("Unable to match any symbol to " + matrix);
            }

            return match;
        }

        public List<Symbol> Spell(IEnumerable<FeatureMatrix> segments)
        {
            return segments.ToList().ConvertAll(fm => this.Spell(fm));
        }

        public string MakeString(IEnumerable<FeatureMatrix> segments)
        {
            StringBuilder str = new StringBuilder();
            foreach (var s in Spell(segments))
            {
                str.Append(s.ToString());
            }
            return str.ToString();
        }

        public List<Symbol> SplitSymbols(string word)
        {
            var list = new List<Symbol>();

            // search through the symbols for the input word, taking the longest
            // possible existing symbol every time

            while (word.Length > 0)
            {
                string symbol = word;
                while (symbol.Length > 0 && !this.ContainsKey(symbol)) 
                {
                    symbol = symbol.Substring(0, symbol.Length - 1);
                }

                if (symbol.Length == 0)
                {
                    // no symbol was found, which is bad guano
                    throw new SpellingException("Couldn't find symbol to match '" + word + "'");
                }

                list.Add(this[symbol]);

                word = word.Substring(symbol.Length);
            }

            return list;
        }

        public List<FeatureMatrix> Pronounce(string word)
        {
            return SplitSymbols(word).ConvertAll(s => s.FeatureMatrix);
        }

        public override string ToString()
        {
            return "SymbolSet";
        }
    }

}
