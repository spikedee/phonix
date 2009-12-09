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
            var log = new Logger(Level.Verbose, Level.Error, Console.Out, phono);
            log.Start();

            try
            {
                var word = new Word(phono.SymbolSet.Pronounce(input));
                phono.RuleSet.ApplyAll(word);
                var output = phono.SymbolSet.MakeString(word);
                Assert.AreEqual(expectedOutput, output);
            }
            finally
            {
                log.Stop();
            }
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
            Action<AbstractFeatureValue> tracer = (fv) => 
            {
                gotTrace = true;
                undef = fv.Feature;
            };
            Trace.OnUndefinedVariableUsed += tracer;

            try
            {
                var phono = ParseWithStdImports("rule voice-assimilate [] => [$vc] / _ []");
                ApplyRules(phono, "sz", "sz");
                Assert.IsTrue(gotTrace);
                Assert.AreSame(phono.FeatureSet.Get<Feature>("vc"), undef);
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

        [Test]
        public void NodeFeature()
        {
            var phono = ParseWithStdImports("feature Height (type=node children=hi,lo)");
            Assert.IsTrue(phono.FeatureSet.Has<NodeFeature>("Coronal"));

            var node = phono.FeatureSet.Get<NodeFeature>("Height");
            var children = new List<Feature>(node.Children);
            Assert.IsTrue(children.Contains(phono.FeatureSet.Get<Feature>("hi")));
            Assert.IsTrue(children.Contains(phono.FeatureSet.Get<Feature>("lo")));
        }

        [Test]
        public void NodeExistsInRule()
        {
            var phono = ParseWithStdImports(
                    @"rule coronal-test [Coronal] => [+vc]");
            ApplyRules(phono, "ptk", "pdk");
        }

        [Test]
        public void NodeVariableInRule()
        {
            var phono = ParseWithStdImports(
                    @"rule coronal-test [] => [$Coronal] / _ [$Coronal -vc]");
            ApplyRules(phono, "TCg", "CCg");
        }

        [Test]
        public void NodeNullInRule()
        {
            var phono = ParseWithStdImports(
                    @"rule coronal-null [Coronal] => [*Place] / _ ");
            ApplyRules(phono, "fTx", "fhx");
        }

        [Test]
        public void LeftwardInsert()
        {
            var phono = ParseWithStdImports("rule leftward-insert (direction=right-to-left) * => c / b _ b");
            ApplyRules(phono, "abba", "abcba");
        }

        [Test]
        public void RightwardInsert()
        {
            var phono = ParseWithStdImports("rule rightward-insert (direction=left-to-right) * => c / b _ b");
            ApplyRules(phono, "abba", "abcba");
        }

        [Test]
        public void BasicExclude()
        {
            var phono = ParseWithStdImports("rule ex a => b / _ c // _ cc");
            ApplyRules(phono, "ac", "bc");
            ApplyRules(phono, "acc", "acc");
        }

        [Test]
        public void ExcludeContextLonger()
        {
            var phono = ParseWithStdImports("rule ex a => b / c[-vc] _  // c _");
            ApplyRules(phono, "csa", "csb");
            ApplyRules(phono, "cca", "cca");
        }

        [Test]
        public void ExcludeContextShorter()
        {
            var phono = ParseWithStdImports("rule ex a => b / k _  // sk _");
            ApplyRules(phono, "ka", "kb");
            ApplyRules(phono, "ska", "ska");
        }

        [Test]
        public void ExcludeNoContext()
        {
            var phono = ParseWithStdImports("rule ex a => b // c _");
            ApplyRules(phono, "ka", "kb");
            ApplyRules(phono, "ca", "ca");
        }

        [Test]
        public void ContextTrailingSlash()
        {
            var phono = ParseWithStdImports("rule ex a => b / _ c / ");
            ApplyRules(phono, "aac", "abc");
        }

        [Test]
        public void ExcludeSingleSlash()
        {
            var phono = ParseWithStdImports("rule ex a => b / _ c / a _ ");
            ApplyRules(phono, "aac", "aac");
        }

        [Test]
        public void Insert()
        {
            // middle
            var phono = ParseWithStdImports("rule insert * => a / b _ b");
            ApplyRules(phono, "bb", "bab");

            // beginning
            phono = ParseWithStdImports("rule insert * => a / _ bb");
            ApplyRules(phono, "bb", "abb");

            // end
            phono = ParseWithStdImports("rule insert * => a / bb _");
            ApplyRules(phono, "bb", "bba");
        }

        [Test]
        public void Delete()
        {
            // middle
            var phono = ParseWithStdImports("rule delete a => * / b _ b");
            ApplyRules(phono, "bab", "bb");

            // beginning
            phono = ParseWithStdImports("rule delete a => * / _ bb");
            ApplyRules(phono, "abb", "bb");

            // end
            phono = ParseWithStdImports("rule delete a => * / bb _");
            ApplyRules(phono, "bba", "bb");
        }
    }
}
