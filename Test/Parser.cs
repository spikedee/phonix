using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phonix;
using Phonix.Parse;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class yyParserTest
    {
        public Phonology ParseWithStdImports(string toParse)
        {
            StringBuilder str = new StringBuilder();
            str.Append("import std.features\nimport std.symbols\n");
            str.Append(toParse);

            var phono = new Phonology();
            PhonixParser.ParseString(phono, str.ToString());

            return phono;
        }

        public void ApplyRules(Phonology phono, string input, string expectedOutput)
        {
            var log = new Log(Log.Level.Verbose, Log.Level.Error, Console.Out, phono);
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

        public void ApplySyllableRule(Phonology phono, string input, string syllableOutput)
        {
            var log = new Log(Log.Level.Verbose, Log.Level.Error, Console.Out, phono);
            log.Start();

            try
            {
                var word = new Word(phono.SymbolSet.Pronounce(input));
                phono.RuleSet.ApplyAll(word);
                Assert.AreEqual(syllableOutput, SyllableBuilderTest.ShowSyllables(phono.SymbolSet, word));
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
            IFeatureValue undef = null;
            Action<Rule, IFeatureValue> tracer = (r, fv) => 
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
            var phono = ParseWithStdImports("rule leftward (direction=right-to-left) a => b / a _");
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
            var phono = ParseWithStdImports("rule delete-middle a => * / b _ b");
            ApplyRules(phono, "bab", "bb");

            // beginning
            phono = ParseWithStdImports("rule delete-beginning a => * / _ bb");
            ApplyRules(phono, "abb", "bb");

            // end
            phono = ParseWithStdImports("rule delete-end a => * / bb _");
            ApplyRules(phono, "bba", "bb");

            // two
            phono = ParseWithStdImports("rule delete-double aa => **");
            ApplyRules(phono, "baab", "bb");
        }

        [Test]
        public void InsertDeleteInvalid()
        {
            try
            {
                ParseWithStdImports("rule insert-delete * a => b * ");
                Assert.Fail("should have thrown exception");
            }
            catch (ParseException) {}
            try
            {
                ParseWithStdImports("rule delete-insert a * => * b ");
                Assert.Fail("should have thrown exception");
            }
            catch (ParseException) {}
            try
            {
                ParseWithStdImports("rule delete-insert a * => b * ");
                Assert.Fail("should have thrown exception");
            }
            catch (ParseException) {}
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
        public void SegmentRepeatZeroOrOnePre()
        {
            var phono = ParseWithStdImports("rule matchany a => x / (c)b _ $ ");
            ApplyRules(phono, "cba", "cbx");
            ApplyRules(phono, "ba", "bx");
            ApplyRules(phono, "ca", "ca");
        }

        [Test]
        public void SegmentRepeatZeroOnMorePre()
        {
            var phono = ParseWithStdImports("rule matchany a => x / (c)*b _ $ ");
            ApplyRules(phono, "cba", "cbx");
            ApplyRules(phono, "ccba", "ccbx");
            ApplyRules(phono, "cccba", "cccbx");
            ApplyRules(phono, "ba", "bx");
            ApplyRules(phono, "ca", "ca");
        }

        [Test]
        public void SegmentRepeatOneOrMorePre()
        {
            var phono = ParseWithStdImports("rule matchany a => x / (c)+b _ $ ");
            ApplyRules(phono, "cba", "cbx");
            ApplyRules(phono, "ccba", "ccbx");
            ApplyRules(phono, "cccba", "cccbx");
            ApplyRules(phono, "ba", "ba");
            ApplyRules(phono, "ca", "ca");
        }

        [Test]
        public void RuleApplicationRate()
        {
            var phono = ParseWithStdImports("rule sporadic (applicationRate=0.25) a => b");
            var rule = phono.RuleSet.OrderedRules.First();
            Assert.AreEqual(0.25, ((Rule) rule).ApplicationRate);
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
            "feature sc (type=scalar min=0 max=3) " +
            "symbol scX [*sc] " +
            "symbol sc0 [sc=0] " +
            "symbol sc1 [sc=1] " +
            "symbol sc2 [sc=2] " +
            "symbol sc3 [sc=3] ";

        [Test]
        public void ScalarNotEq()
        {
            var phono = new Phonology();
            PhonixParser.ParseString(phono, scalarDefs + "rule neq [sc<>0] => sc2");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc2");
            ApplyRules(phono, "sc2", "sc2");
            ApplyRules(phono, "sc3", "sc2");
            ApplyRules(phono, "scX", "sc2");
        }

        [Test]
        public void ScalarGT()
        {
            var phono = new Phonology();
            PhonixParser.ParseString(phono, scalarDefs + "rule gt [sc>1] => sc0");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc1");
            ApplyRules(phono, "sc2", "sc0");
            ApplyRules(phono, "sc3", "sc0");
            ApplyRules(phono, "scX", "scX");
        }

        [Test]
        public void ScalarGTOrEq()
        {
            var phono = new Phonology();
            PhonixParser.ParseString(phono, scalarDefs + "rule gte [sc>=2] => sc0");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc1");
            ApplyRules(phono, "sc2", "sc0");
            ApplyRules(phono, "sc3", "sc0");
            ApplyRules(phono, "scX", "scX");
        }

        [Test]
        public void ScalarLT()
        {
            var phono = new Phonology();
            PhonixParser.ParseString(phono, scalarDefs + "rule lt [sc<2] => sc3");
            ApplyRules(phono, "sc0", "sc3");
            ApplyRules(phono, "sc1", "sc3");
            ApplyRules(phono, "sc2", "sc2");
            ApplyRules(phono, "sc3", "sc3");
            ApplyRules(phono, "scX", "scX");
        }

        [Test]
        public void ScalarLTOrEq()
        {
            var phono = new Phonology();
            PhonixParser.ParseString(phono, scalarDefs + "rule lte [sc<=1] => sc3");
            ApplyRules(phono, "sc0", "sc3");
            ApplyRules(phono, "sc1", "sc3");
            ApplyRules(phono, "sc2", "sc2");
            ApplyRules(phono, "sc3", "sc3");
            ApplyRules(phono, "scX", "scX");
        }

        [Test]
        public void ScalarAdd()
        {
            var phono = new Phonology();
            PhonixParser.ParseString(phono, scalarDefs + "rule add [sc<3] => [sc=+1]");
            ApplyRules(phono, "sc0", "sc1");
            ApplyRules(phono, "sc1", "sc2");
            ApplyRules(phono, "sc2", "sc3");
            ApplyRules(phono, "sc3", "sc3");
            ApplyRules(phono, "scX", "scX");
        }

        [Test]
        public void ScalarSubtract()
        {
            var phono = new Phonology();
            PhonixParser.ParseString(phono, scalarDefs + "rule subtract [sc>0] => [sc=-1]");
            ApplyRules(phono, "sc0", "sc0");
            ApplyRules(phono, "sc1", "sc0");
            ApplyRules(phono, "sc2", "sc1");
            ApplyRules(phono, "sc3", "sc2");
            ApplyRules(phono, "scX", "scX");
        }

        [Test]
        public void ScalarValueOutOfRange()
        {
            var phono = new Phonology();

            bool gotTrace = false;
            Rule rule = null;
            ScalarFeature feature = null;
            int traceValue = 100;
            Action<Rule, ScalarFeature, int> tracer = (r, f, i) =>
            { 
                gotTrace = true;
                rule = r;
                feature = f;
                traceValue = i;
            };

            PhonixParser.ParseString(phono, scalarDefs + "rule subtract [] => [sc=-1]");
            phono.RuleSet.ScalarValueRangeViolation += tracer;

            ApplyRules(phono, "sc0", "sc0"); // assert that this doesn't blow up

            Assert.IsTrue(gotTrace);
            Assert.AreSame(rule, phono.RuleSet.OrderedRules.Where(r => r.Name.Equals("subtract")).First());
            Assert.AreSame(feature, phono.FeatureSet.Get<Feature>("sc"));
            Assert.AreEqual(-1, traceValue);
        }

        [Test]
        public void ScalarValueInvalidOp()
        {
            var phono = new Phonology();

            bool gotTrace = false;
            Rule rule = null;
            ScalarFeature feature = null;
            Action<Rule, ScalarFeature, string> tracer = (r, f, s) =>
            { 
                gotTrace = true;
                rule = r;
                feature = f;
            };

            PhonixParser.ParseString(phono, scalarDefs + "rule subtract [] => [sc=-1]");
            phono.RuleSet.InvalidScalarValueOp += tracer;

            ApplyRules(phono, "scX", "scX"); // this shouldn't throw an exception

            Assert.IsTrue(gotTrace);
            Assert.AreSame(rule, phono.RuleSet.OrderedRules.Where(r => r.Name.Equals("subtract")).First());
            Assert.AreSame(feature, phono.FeatureSet.Get<Feature>("sc"));
        }

        [Test]
        public void SyllableCV()
        {
            var phono = ParseWithStdImports("syllable onset [+cons] nucleus [-cons +son]");
            ApplySyllableRule(phono, "basiho", "<b:a><s:i><h:o>");
            ApplySyllableRule(phono, "asiho", "<a><s:i><h:o>");
            ApplySyllableRule(phono, "basih", "<b:a><s:i>h");
        }

        [Test]
        public void SyllableCVC()
        {
            var phono = ParseWithStdImports("syllable onset [+cons] nucleus [-cons +son] coda [+son]");
            ApplySyllableRule(phono, "basihon", "<b:a><s:i><h:o.n>");
            ApplySyllableRule(phono, "asihon", "<a><s:i><h:o.n>");
            ApplySyllableRule(phono, "basih", "<b:a><s:i>h");
            ApplySyllableRule(phono, "bai", "<b:a.i>");
        }

        [Test]
        public void SyllableCVCOnsetRequired()
        {
            var phono = ParseWithStdImports("syllable (onsetRequired) onset [+cons] nucleus [-cons +son] coda [+son]");
            ApplySyllableRule(phono, "basihon", "<b:a><s:i><h:o.n>");
            ApplySyllableRule(phono, "asihon", "a<s:i><h:o.n>");
            ApplySyllableRule(phono, "basih", "<b:a><s:i>h");
            ApplySyllableRule(phono, "bai", "<b:a.i>");
        }

        [Test]
        public void SyllableCVCCodaRequired()
        {
            var phono = ParseWithStdImports("syllable (codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons]");
            ApplySyllableRule(phono, "basihon", "<b:a.s><i.h><o.n>");
            ApplySyllableRule(phono, "asihon", "<a.s><i.h><o.n>");
            ApplySyllableRule(phono, "basih", "<b:a.s><i.h>");
            ApplySyllableRule(phono, "bai", "bai");
        }

        [Test]
        public void SyllableCVCOnsetAndCodaRequired()
        {
            var phono = ParseWithStdImports(
                    "syllable (onsetRequired codaRequired) " + 
                    "onset [+cons] nucleus [-cons +son] coda [+cons]");
            ApplySyllableRule(phono, "basihon", "<b:a.s>i<h:o.n>");
            ApplySyllableRule(phono, "asihon", "a<s:i.h>on");
            ApplySyllableRule(phono, "basih", "<b:a.s>ih");
            ApplySyllableRule(phono, "bai", "bai");
        }

        [Test]
        public void SyllableCCV()
        {
            var phono = ParseWithStdImports("syllable onset [+cons]([+cons]) nucleus [-cons +son]");
            ApplySyllableRule(phono, "basiho", "<b:a><s:i><h:o>");
            ApplySyllableRule(phono, "brastihno", "<br:a><st:i><hn:o>");
            ApplySyllableRule(phono, "aszihxon", "<a><sz:i><hx:o>n");
            ApplySyllableRule(phono, "barsih", "<b:a><rs:i>h");
        }

        [Test]
        public void SyllableCCVC()
        {
            var phono = ParseWithStdImports(
                    "syllable onset ([-son])([+cons]) nucleus [-cons +son] coda [+son]");
            ApplySyllableRule(phono, "basihon", "<b:a><s:i><h:o.n>");
            ApplySyllableRule(phono, "bramstihnorl", "<br:a.m><st:i><hn:o.r>l");
            ApplySyllableRule(phono, "alszihxon", "<a.l><sz:i><hx:o.n>");
            ApplySyllableRule(phono, "barsih", "<b:a.r><s:i>h");
            ApplySyllableRule(phono, "btai", "<bt:a.i>");
        }

        [Test]
        public void SyllableCCVCC()
        {
            var phono = ParseWithStdImports(
                    "syllable onset ([-son])([+cons]) nucleus [-cons +son] coda ([+son])([+cons])");
            ApplySyllableRule(phono, "basihon", "<b:a><s:i><h:o.n>");
            ApplySyllableRule(phono, "bramstihnorl", "<br:a.m><st:i><hn:o.rl>");
            ApplySyllableRule(phono, "alszihxon", "<a.l><sz:i><hx:o.n>");
            ApplySyllableRule(phono, "barsih", "<b:a.r><s:i.h>");
            ApplySyllableRule(phono, "btaif", "<bt:a.if>");
        }

        [Test]
        public void SyllableCVCNucleusRight()
        {
            var phono = ParseWithStdImports(
                    "syllable (nucleusPreference=right) " + 
                    "onset [+cons]([+son]) nucleus [-cons +son] coda []");
            ApplySyllableRule(phono, "bui", "<bu:i>");
            ApplySyllableRule(phono, "biu", "<bi:u>");
        }

        [Test]
        public void SyllableCVCNucleusLeft()
        {
            var phono = ParseWithStdImports(
                    "syllable (nucleusPreference=left) " + 
                    "onset [+cons][+son] onset [+cons] nucleus [-cons +son] coda []");
            ApplySyllableRule(phono, "bui", "<b:u.i>");
            ApplySyllableRule(phono, "biu", "<b:i.u>");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void SyllableInvalidNoSegmentInOnset()
        {
            ParseWithStdImports("syllable onset nucleus [] coda []");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void SyllableInvalidNoSegmentInNucleus()
        {
            ParseWithStdImports("syllable onset [] nucleus coda []");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void SyllableInvalidNoSegmentInCoda()
        {
            ParseWithStdImports("syllable onset [] nucleus [] coda ");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void SyllableInvalidRegexPlus()
        {
            ParseWithStdImports("syllable onset ([])+ nucleus [] coda []");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void SyllableInvalidRegexStar()
        {
            ParseWithStdImports("syllable onset ([])* nucleus [] coda []");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void SyllableInvalidParameter()
        {
            ParseWithStdImports("syllable (invalid) onset [] nucleus [] coda []");
        }

        [Test]
        public void MatchOnset()
        {
            var phono = ParseWithStdImports(
                    "syllable onset [+cons]([+cons]) nucleus [-cons +son] coda [+cons] " +
                    "rule markOnset [<onset>] => x / _ [<*onset>]");
            ApplyRules(phono, "basiho", "xaxixo");
            ApplyRules(phono, "brastihno", "bxasxihxo");
            ApplyRules(phono, "aszihgon", "asxihxon");
            ApplyRules(phono, "barsih", "xarxih");
        }

        [Test]
        public void MatchCoda()
        {
            var phono = ParseWithStdImports(
                    "syllable onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markCoda [<coda>] => x / [<*coda>] _ ");
            ApplyRules(phono, "basiho", "basiho");
            ApplyRules(phono, "brastihno", "braxtixno");
            ApplyRules(phono, "aszihgon", "axzixgox");
            ApplyRules(phono, "barsih", "baxsix");
        }

        [Test]
        public void MatchNucleus()
        {
            var phono = ParseWithStdImports(
                    "syllable onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markCoda [<nucleus>] => x ");
            ApplyRules(phono, "basiho", "bxsxhx");
            ApplyRules(phono, "brastihno", "brxstxhnx");
            ApplyRules(phono, "aszihgon", "xszxhgxn");
            ApplyRules(phono, "barsih", "bxrsxh");
        }

        [Test]
        public void MatchSyllable()
        {
            var phono = ParseWithStdImports(
                    "syllable (onsetRequired codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markSyllable [<syllable>] => x ");
            ApplyRules(phono, "basiho", "xxxiho");
            ApplyRules(phono, "brastihno", "bxxxxxxno");
            ApplyRules(phono, "aszihgon", "asxxxxxx");
            ApplyRules(phono, "barsih", "xxxxxx");
        }

        [Test]
        public void MatchNoSyllable()
        {
            var phono = ParseWithStdImports(
                    "syllable (onsetRequired codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markNoSyllable [<*syllable>] => x ");
            ApplyRules(phono, "basiho", "basxxx");
            ApplyRules(phono, "brastihno", "xrastihxx");
            ApplyRules(phono, "aszihgon", "xxzihgon");
            ApplyRules(phono, "barsih", "barsih");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void UnmatchedLeftBracket()
        {
            ParseWithStdImports("rule markInvalid [<syllable] => x");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void UnmatchedRightBracket()
        {
            ParseWithStdImports("rule markInvalid [syllable>] => x");
        }

        [Test]
        [ExpectedException(typeof(ParseException))]
        public void UnrecognizedTierName()
        {
            ParseWithStdImports("rule markInvalid [<wrong>] => x");
        }
    }
}
