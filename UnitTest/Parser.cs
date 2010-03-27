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
                var output = Shell.SafeMakeString(phono, word, Console.Out);
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
            IMatchCombine undef = null;
            Action<Rule, IMatchCombine> tracer = (r, fv) => 
            {
                gotTrace = true;
                undef = fv;
            };

            var phono = ParseWithStdImports("rule voice-assimilate [] => [$vc] / _ []");

            phono.RuleSet.UndefinedVariableUsed += tracer;
            ApplyRules(phono, "sz", "sz");

            Assert.IsTrue(gotTrace);
            Assert.AreSame(phono.FeatureSet.Get<Feature>("vc").VariableValue, undef);
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

        [Test]
        public void RulePersist()
        {
            var phono = ParseWithStdImports("rule persist-b-a (persist) b => a   rule a-b a => b");
            ApplyRules(phono, "baa", "aaa");
        }

        [Test]
        public void SymbolDiacritic()
        {
            var phono = ParseWithStdImports("symbol ~ (diacritic) [+nas]");
            Assert.AreEqual(1, phono.SymbolSet.Diacritics.Count);
            Assert.IsTrue(phono.SymbolSet.Diacritics.ContainsKey("~"));
        }

        [Test]
        public void SegmentRepeatZeroOrMore()
        {
            var phono = ParseWithStdImports("rule matchany a => c / _ (b)*$ ");
            ApplyRules(phono, "a", "c");
            ApplyRules(phono, "ab", "cb");
            ApplyRules(phono, "abb", "cbb");
            ApplyRules(phono, "ac", "ac");
        }

        [Test]
        public void SegmentRepeatOneOrMore()
        {
            var phono = ParseWithStdImports("rule matchany a => c / _ (b)+$ ");
            ApplyRules(phono, "a", "a");
            ApplyRules(phono, "ab", "cb");
            ApplyRules(phono, "abb", "cbb");
            ApplyRules(phono, "ac", "ac");
        }

        [Test]
        public void SegmentRepeatZeroOrOne()
        {
            var phono = ParseWithStdImports("rule matchany a => c / _ (b)$ ");
            ApplyRules(phono, "a", "c");
            ApplyRules(phono, "ab", "cb");
            ApplyRules(phono, "abb", "abb");
            ApplyRules(phono, "ac", "ac");
        }

        [Test]
        public void MultipleSegmentOptional()
        {
            var phono = ParseWithStdImports("rule matchany a => c / _ (bc)c$ ");
            ApplyRules(phono, "a", "a");
            ApplyRules(phono, "ac", "cc");
            ApplyRules(phono, "abc", "abc");
            ApplyRules(phono, "abcc", "cbcc");
        }

        [Test]
        public void SegmentOptional()
        {
            var phono = ParseWithStdImports("rule matchany a => c / _ (b)+c$ ");
            ApplyRules(phono, "a", "a");
            ApplyRules(phono, "ac", "ac");
            ApplyRules(phono, "abc", "cbc");
            ApplyRules(phono, "abbc", "cbbc");
        }

        [Test]
        public void MultipleSegmentOptionalBacktrack()
        {
            var phono = ParseWithStdImports("rule matchany a => c / _ (bc)+b$ ");
            ApplyRules(phono, "abc", "abc");
            ApplyRules(phono, "abcb", "cbcb");
            ApplyRules(phono, "abcbc", "abcbc");
            ApplyRules(phono, "abcbcb", "cbcbcb");
        }

        [Test]
        public void RuleApplicationRate()
        {
            var phono = ParseWithStdImports("rule sporadic (applicationRate=0.25) a => b");
            var rule = phono.RuleSet.OrderedRules.First();
            Assert.AreEqual(0.25, rule.ApplicationRate);
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void RuleApplicationRateOutOfRange()
        {
            ParseWithStdImports("rule sporadic (applicationRate=1.25) a => b");
            Assert.Fail("Shouldn't reach this line");
        }

        [Test]
        public void ScalarRange()
        {
            var phono = ParseWithStdImports("feature scRange (type=scalar min=1 max=4)");
            Assert.IsTrue(phono.FeatureSet.Has<ScalarFeature>("scRange"));

            var sc = phono.FeatureSet.Get<ScalarFeature>("scRange");
            Assert.AreEqual(1, sc.Min.Value);
            Assert.AreEqual(4, sc.Max.Value);
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void ScalarMissingMin()
        {
            ParseWithStdImports("feature scRange (type=scalar max=4)");
            Assert.Fail("Shouldn't reach this line");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void ScalarMissingMax()
        {
            ParseWithStdImports("feature scRange (type=scalar min=1)");
            Assert.Fail("Shouldn't reach this line");
        }

        private string scalarDefs = 
            "feature sc (type=scalar) " +
            "symbol sc0 [sc=0] " +
            "symbol sc1 [sc=1] " +
            "symbol sc2 [sc=2] " +
            "symbol sc3 [sc=3] ";

        [Test]
        public void ScalarNotEq()
        {
            var phono = new Phonology();
            Parse.Util.ParseString(phono, scalarDefs + "rule neq [sc<>0] => sc2");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc2");
            ApplyRules(phono, "sc2", "sc2");
            ApplyRules(phono, "sc3", "sc2");
        }

        [Test]
        public void ScalarGT()
        {
            var phono = new Phonology();
            Parse.Util.ParseString(phono, scalarDefs + "rule gt [sc>1] => sc0");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc1");
            ApplyRules(phono, "sc2", "sc0");
            ApplyRules(phono, "sc3", "sc0");
        }

        [Test]
        public void ScalarGTOrEq()
        {
            var phono = new Phonology();
            Parse.Util.ParseString(phono, scalarDefs + "rule gte [sc>=2] => sc0");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc1");
            ApplyRules(phono, "sc2", "sc0");
            ApplyRules(phono, "sc3", "sc0");
        }

        [Test]
        public void ScalarLT()
        {
            var phono = new Phonology();
            Parse.Util.ParseString(phono, scalarDefs + "rule lt [sc<2] => sc3");
            ApplyRules(phono, "sc0", "sc3");
            ApplyRules(phono, "sc1", "sc3");
            ApplyRules(phono, "sc2", "sc2");
            ApplyRules(phono, "sc3", "sc3");
        }

        [Test]
        public void ScalarLTOrEq()
        {
            var phono = new Phonology();
            Parse.Util.ParseString(phono, scalarDefs + "rule lte [sc<=1] => sc3");
            ApplyRules(phono, "sc0", "sc3");
            ApplyRules(phono, "sc1", "sc3");
            ApplyRules(phono, "sc2", "sc2");
            ApplyRules(phono, "sc3", "sc3");
        }

        [Test]
        public void ScalarAdd()
        {
            var phono = new Phonology();
            Parse.Util.ParseString(phono, scalarDefs + "rule add [sc<3] => [sc=+1]");
            ApplyRules(phono, "sc0", "sc1");
            ApplyRules(phono, "sc1", "sc2");
            ApplyRules(phono, "sc2", "sc3");
            ApplyRules(phono, "sc3", "sc3");
        }

        [Test]
        public void ScalarSubtract()
        {
            var phono = new Phonology();
            Parse.Util.ParseString(phono, scalarDefs + "rule subtract [sc>0] => [sc=-1]");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc0");
            ApplyRules(phono, "sc2", "sc1");
            ApplyRules(phono, "sc3", "sc2");
        }
    }
}
