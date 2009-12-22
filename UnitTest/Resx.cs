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
        private void ParseResource(params string[] resources)
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


    }
}
