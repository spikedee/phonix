using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
    internal class SymbolTable : Dictionary<string, Symbol>
    {
        public event Action<Symbol> SymbolDefined;
        public event Action<Symbol, Symbol> SymbolRedefined;
        public event Action<Symbol, Symbol> SymbolDuplicate;

        private int _longestSymbol = 0;

        public SymbolTable()
        {
            // add null event handlers
            SymbolDefined += (s) => {};
            SymbolRedefined += (s1, s2) => {};
            SymbolDuplicate += (s1, s2) => {};
        }

        public void Add(Symbol s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            SymbolDefined(s);

            if (this.ContainsKey(s.Label))
            {
                SymbolRedefined(this[s.Label], s);
            }

            foreach (var existing in Values)
            {
                if (existing.FeatureMatrix.Equals(s.FeatureMatrix))
                {
                    SymbolDuplicate(existing, s);
                }
            }

            if (s.Label.Length > _longestSymbol)
            {
                _longestSymbol = s.Label.Length;
            }

            this[s.Label] = s;
        }

        // Take as many symbols as are recognized can from `word`, and return
        // them. The out param `leftover` contains the unrecognized portion of
        // `word`.
        public List<Symbol> TakeSymbols(string word, out string leftover)
        {
            var list = new List<Symbol>();

            // search through the symbols for the input word, taking the longest
            // possible existing symbol every time

            while (word.Length > 0)
            {
                string symbol = word.Substring(0, Math.Min(_longestSymbol, word.Length));
                while (symbol.Length > 0 && !this.ContainsKey(symbol)) 
                {
                    symbol = symbol.Substring(0, symbol.Length - 1);
                }

                if (symbol.Length == 0)
                {
                    // we can't find any more symbols in this string
                    break;
                }

                list.Add(this[symbol]);

                word = word.Substring(symbol.Length);
            }

            leftover = word;
            return list;
        }
    }

    public class SymbolSet : IEnumerable<Symbol>
    {
        internal readonly SymbolTable BaseSymbols = new SymbolTable();
        internal readonly SymbolTable Diacritics = new SymbolTable();

        public event Action<Symbol> SymbolDefined;
        public event Action<Symbol, Symbol> SymbolRedefined;
        public event Action<Symbol, Symbol> SymbolDuplicate;

        public SymbolSet()
        {
            // add null event handlers
            SymbolDefined += (s) => {};
            SymbolRedefined += (s1, s2) => {};
            SymbolDuplicate += (s1, s2) => {};
            
            // Handle events coming from symbol tables
            BaseSymbols.SymbolDefined += s => SymbolDefined(s);
            BaseSymbols.SymbolRedefined += (s1, s2) => SymbolRedefined(s1, s2);
            BaseSymbols.SymbolDuplicate += (s1, s2) => SymbolDuplicate(s1, s2);

            Diacritics.SymbolDefined += s => SymbolDefined(s);
            Diacritics.SymbolRedefined += (s1, s2) => SymbolRedefined(s1, s2);
            Diacritics.SymbolDuplicate += (s1, s2) => SymbolDuplicate(s1, s2);
        }

        public int Count
        {
            get
            {
                return BaseSymbols.Count + Diacritics.Count;
            }
        }

#region IEnumerable members

        public IEnumerator<Symbol> GetEnumerator()
        {
            foreach (Symbol s in BaseSymbols.Values)
            {
                yield return s;
            }
            foreach (Symbol s in Diacritics.Values)
            {
                yield return s;
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion


        public void Add(Symbol s)
        {
            BaseSymbols.Add(s);
        }

        public void AddDiacritic(Symbol s)
        {
            Diacritics.Add(s);
        }

        public Symbol Spell(FeatureMatrix matrix)
        {
            // TODO: this is the hard one
            foreach (var s in BaseSymbols.Values)
            {
                if (s.Matches(null, matrix))
                {
                    return s;
                }
            }
            throw new SpellingException("Unable to match any symbol to " + matrix);
        }

        public List<Symbol> Spell(IEnumerable<FeatureMatrix> segments)
        {
            return segments.ToList().ConvertAll(fm => this.Spell(fm));
        }

        public List<Symbol> SplitSymbols(string word)
        {
            var list = new List<Symbol>();

            // diacritics follow the base symbol. so match as many base symbols
            // as you can, then diacritics, then repeat. if we ever match zero
            // base symbols, then we have bad input.
            while (word.Length > 0)
            {
                var baseSymbols = BaseSymbols.TakeSymbols(word, out word);
                if (baseSymbols.Count == 0)
                {
                    // bad mojo--couldn't recognize any symbols here
                    throw new SpellingException("Couldn't find symbol to match '" + word + "'");
                }
                list.AddRange(baseSymbols);

                if (word.Length > 0)
                {
                    list.AddRange(Diacritics.TakeSymbols(word, out word));
                }
            }

            return list;
        }

        public List<FeatureMatrix> Pronounce(string word)
        {
            return SplitSymbols(word).ConvertAll(s => s.FeatureMatrix);
        }
    }
}
