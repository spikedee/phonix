using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phonix;

namespace Phonix.UnitTest
{
    using NUnit.Framework;

    [TestFixture]
    public class ParserTest
    {
        public Phonology ParseWithStdImports(string toParse)
        {
            StringBuilder str = new StringBuilder();
            str.Append("import std.features\nimport std.symbols\n");
            str.Append(toParse);

            var phono = new Phonology();
            Parse.Util.ParseString(phono, str.ToString());

            return phono;
        }

        public void ApplyRules(Phonology phono, string input, string expectedOutput)
        {
            var word = new Word(phono.SymbolSet.Pronounce(input));
            phono.RuleSet.ApplyAll(word);
            var output = phono.SymbolSet.MakeString(word);
            Assert.AreEqual(expectedOutput, output);
        }

        [Test]
        public void RuleWithVariable()
        {
            var phono = ParseWithStdImports("rule voice-assimilate [] => [$vc] / _ [$vc]");
            ApplyRules(phono, "sz", "zz");
        }

        [Test]
        public void RuleWithVariableUndefined()
        {
            bool gotTrace = false;
            Feature undef = null;
            Action<FeatureValueBase> tracer = (fv) => 
            {
                gotTrace = true;
                undef = fv.Feature;
            };
            Trace.OnUndefinedVariableUsed += tracer;

            try
            {
                var phono = ParseWithStdImports("rule voice-assimilate [] => [$ro] / _ []");
                ApplyRules(phono, "sz", "sz");
                Assert.IsTrue(gotTrace);
                Assert.AreSame(phono.FeatureSet.Get<Feature>("ro"), undef);
            }
            finally
            {
                Trace.OnUndefinedVariableUsed -= tracer;
            }
        }

        [Test]
        public void RuleDirectionRightward()
        {
            // default direction should be rightward
            var phono = ParseWithStdImports("rule rightward a => b / a _");
            ApplyRules(phono, "aaa", "aba");

            var phono2 = ParseWithStdImports("rule rightward (direction=left-to-right) a => b / a _");
            ApplyRules(phono2, "aaa", "aba");
        }

        [Test]
        public void RuleDirectionLeftward()
        {
            var phono = ParseWithStdImports("rule rightward (direction=right-to-left) a => b / a _");
            ApplyRules(phono, "aaa", "abb");
        }
    }
}
