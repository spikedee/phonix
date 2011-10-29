using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;
using Phonix.Parse;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class ResxTest
    {
        private Phonology ParseResource(params string[] resources)
        {
            Phonology phono = new Phonology();

            phono.FeatureSet.FeatureRedefined += (f1, f2) => 
            { 
                Assert.Fail("Duplicate features: {0}, {1}", f1, f2);
            };

            phono.SymbolSet.SymbolRedefined += (s1, s2) => 
            { 
                Assert.Fail("Redefined symbols: {0}, {1}", s1, s2);
            };

            phono.SymbolSet.SymbolDuplicate += (s1, s2) => 
            { 
                Assert.Fail("Duplicate symbols: {0}, {1}", s1, s2);
            };

            foreach (var res in resources)
            {
                var parser = PhonixParser.FileParser(res);
                parser.Parse(phono);
            }

            return phono;
        }

        [Test]
        public void ParseStdFeatures()
        {
            ParseResource("std.features");
        }

        [Test]
        public void ParseStdSymbols()
        {
            ParseResource("std.features", "std.symbols");
        }

        [Test]
        public void ParseStdSymbolsDiacritics()
        {
            ParseResource("std.features", "std.symbols.diacritics");
        }

        [Test]
        public void ParseStdSymbolsIpa()
        {
            ParseResource("std.features", "std.symbols.ipa");
        }

        [Test]
        public void ParseStdSymbolsIpaDiacritics()
        {
            ParseResource("std.features", "std.symbols.ipa.diacritics");
        }

        [Test]
        public void SymbolEquivalence()
        {
            // this test exists to ensure that the symbols in the ascii set and
            // the ipa set are the same

            // set up two phonologies with the same features
            var ascii = ParseResource("std.features");
            var ipa = new Phonology();
            foreach (var f in ascii.FeatureSet)
            {
                ipa.FeatureSet.Add(f);
            }

            var asciiParser = PhonixParser.FileParser("std.symbols");
            asciiParser.Parse(ascii);
            var ipaParser = PhonixParser.FileParser("std.symbols.ipa");
            ipaParser.Parse(ipa);

            Assert.IsTrue(ascii.SymbolSet.BaseSymbols.Count > 0);
            Assert.IsTrue(ascii.SymbolSet.Diacritics.Count == 0);
            Assert.AreEqual(ascii.SymbolSet.Count, ipa.SymbolSet.Count);
            foreach (Symbol s in ascii.SymbolSet)
            {
                try
                {
                    // just call Spell to ensure that an exact match exists
                    ipa.SymbolSet.Spell(s.FeatureMatrix);
                }
                catch (SpellingException)
                {
                    Assert.Fail("No match in ipa for {0} {1}", s, s.FeatureMatrix);
                }
            }
        }

        [Test]
        public void DiacriticSymbolEquivalence()
        {
            // this test exists to ensure that the diacritics in the ascii set
            // and the ipa set are the same

            // set up two phonologies with the same features
            var ascii = ParseResource("std.features");
            var ipa = new Phonology();
            foreach (var f in ascii.FeatureSet)
            {
                ipa.FeatureSet.Add(f);
            }

            var asciiParser = PhonixParser.FileParser("std.symbols.diacritics");
            asciiParser.Parse(ascii);
            var ipaParser = PhonixParser.FileParser("std.symbols.ipa.diacritics");
            ipaParser.Parse(ipa);

            Assert.IsTrue(ascii.SymbolSet.BaseSymbols.Count == 0);
            Assert.IsTrue(ascii.SymbolSet.Diacritics.Count > 0);
            Assert.AreEqual(ascii.SymbolSet.Count, ipa.SymbolSet.Count);
            foreach (Diacritic d in ascii.SymbolSet.Diacritics.Values)
            {
                Assert.AreEqual(1, 
                        ipa.SymbolSet.Diacritics.Values.Where(s => s.FeatureMatrix.Equals(d.FeatureMatrix)).Count());
            }
        }

    }
}
