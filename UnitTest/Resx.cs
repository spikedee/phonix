using System;
using System.Collections.Generic;
using System.Linq;
using Phonix;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class ResxTest
    {
        private Phonology ParseResource(params string[] resources)
        {
            Action<Feature, Feature> traceFeatureRedef = (f1, f2) => 
            { 
                Assert.Fail("Duplicate features: {0}, {1}", f1, f2);
            };
            Trace.OnFeatureRedefined += traceFeatureRedef;

            Action<Symbol, Symbol> traceSymbolRedef = (s1, s2) => 
            { 
                Assert.Fail("Redefined symbols: {0}, {1}", s1, s2);
            };
            Trace.OnSymbolRedefined += traceSymbolRedef;

            Action<Symbol, Symbol> traceSymbolDup = (s1, s2) => 
            { 
                Assert.Fail("Duplicate symbols: {0}, {1}", s1, s2);
            };
            Trace.OnSymbolDuplicate += traceSymbolDup;

            Phonology phono = new Phonology();
            try
            {
                foreach (var res in resources)
                {
                    Phonix.Parse.Util.ParseFile(phono, "test", res);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            finally
            {
                Trace.OnFeatureRedefined -= traceFeatureRedef;
                Trace.OnSymbolRedefined -= traceSymbolRedef;
                Trace.OnSymbolDuplicate -= traceSymbolDup;
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
        public void ParseStdSymbolsIpa()
        {
            ParseResource("std.features", "std.symbols.ipa");
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

            Phonix.Parse.Util.ParseFile(ascii, "ascii", "std.symbols");
            Phonix.Parse.Util.ParseFile(ipa, "ipa", "std.symbols.ipa");

            Assert.AreEqual(ascii.SymbolSet.Count, ipa.SymbolSet.Count);
            foreach (Symbol s in ascii.SymbolSet.Values)
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

    }
}
