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
        [Test]
        public void ParseStdFeatures()
        {
            Action<Feature, Feature> traceRedef = (f1, f2) => 
            { 
                Assert.Fail("Duplicate features: {0}, {1}", f1, f2);
            };
            Trace.OnFeatureRedefined += traceRedef;

            try
            {
                Phonology phono = new Phonology();
                Phonix.Parse.Util.ParseFile(phono, "test", "std.features");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            finally
            {
                Trace.OnFeatureRedefined -= traceRedef;
            }
        }

        [Test]
        public void ParseStdSymbols()
        {
            Action<Symbol, Symbol> traceRedef = (s1, s2) => 
            { 
                Assert.Fail("Redefined symbols: {0}, {1}", s1, s2);
            };
            Trace.OnSymbolRedefined += traceRedef;

            Action<Symbol, Symbol> traceDup = (s1, s2) => 
            { 
                Assert.Fail("Duplicate symbols: {0}, {1}", s1, s2);
            };
            Trace.OnSymbolDuplicate += traceDup;
            try
            {
                Phonology phono = new Phonology();
                Phonix.Parse.Util.ParseFile(phono, "test", "std.symbols");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            finally
            {
                Trace.OnSymbolRedefined -= traceRedef;
                Trace.OnSymbolDuplicate -= traceDup;
            }
        }
    }
}
