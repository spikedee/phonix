using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class ParserTest
    {
        public string CreateStringWithStdImports(params string[] input)
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("import std.features");
            str.AppendLine("import std.symbols");

            foreach (var line in input)
            {
                str.AppendLine(line);
            }

            return str.ToString();
        }

        public void RunPhonixFile(string phonixFile, string inputWords, string expectedOutput)
        {
            File.WriteAllText("test.phonix", phonixFile);
            File.WriteAllText("test.in", inputWords);
            File.WriteAllText("test.expected", expectedOutput);

            var proc = Process.Start("phonix", "test.phonix -i test.in -o test.out -v -w");
            proc.WaitForExit();

            CompareFiles("test.expected", "test.out");
        }

        internal static void CompareFiles(string expectedFile, string testFile)
        {
            var expected = File.OpenText(expectedFile);
            var test = File.OpenText(testFile);

            while (!test.EndOfStream)
            {
                string testLine = test.ReadLine();
                string expectedLine = expected.ReadLine();
                Assert.AreEqual(expectedLine, testLine);
            }
            Assert.AreEqual(expected.EndOfStream, test.EndOfStream);
        }

        public void ApplySyllableRule(string file, string input, string syllableOutput)
        {
            // TODO
            throw new NotImplementedException();
        }

        [Test]
        public void RuleWithVariable()
        {
            var phono = CreateStringWithStdImports("rule voice-assimilate [] => [$vc] / _ [$vc]");
            RunPhonixFile(phono, "sz", "zz");
        }

        [Test]
        public void RuleWithVariableUndefined()
        {
            /*
            bool gotTrace = false;
            IFeatureValue undef = null;
            Action<Rule, IFeatureValue> tracer = (r, fv) => 
            {
                gotTrace = true;
                undef = fv;
            };

            var phono = CreateStringWithStdImports("rule voice-assimilate [] => [$vc] / _ []");

            phono.RuleSet.UndefinedVariableUsed += tracer;
            RunPhonixFile(phono, "sz", "sz");

            Assert.IsTrue(gotTrace);
            Assert.AreSame(phono.FeatureSet.Get<Feature>("vc").VariableValue, undef);
            */
        }

        [Test]
        public void RuleDirectionRightward()
        {
            // default direction should be rightward
            var phono = CreateStringWithStdImports("rule rightward a => b / a _");
            RunPhonixFile(phono, "aaa", "aba");

            var phono2 = CreateStringWithStdImports("rule rightward (direction=left-to-right) a => b / a _");
            RunPhonixFile(phono2, "aaa", "aba");
        }

        [Test]
        public void RuleDirectionLeftward()
        {
            var phono = CreateStringWithStdImports("rule leftward (direction=right-to-left) a => b / a _");
            RunPhonixFile(phono, "aaa", "abb");
        }

        [Test]
        public void NodeFeature()
        {
            /*
            var phono = CreateStringWithStdImports("feature Height (type=node children=hi,lo)");
            Assert.IsTrue(phono.FeatureSet.Has<NodeFeature>("Coronal"));

            var node = phono.FeatureSet.Get<NodeFeature>("Height");
            var children = new List<Feature>(node.Children);
            Assert.IsTrue(children.Contains(phono.FeatureSet.Get<Feature>("hi")));
            Assert.IsTrue(children.Contains(phono.FeatureSet.Get<Feature>("lo")));
            */
        }

        [Test]
        public void NodeExistsInRule()
        {
            var phono = CreateStringWithStdImports(
                    @"rule coronal-test [Coronal] => [+vc]");
            RunPhonixFile(phono, "ptk", "pdk");
        }

        [Test]
        public void NodeVariableInRule()
        {
            var phono = CreateStringWithStdImports(
                    @"rule coronal-test [] => [$Coronal] / _ [$Coronal -vc]");
            RunPhonixFile(phono, "TCg", "CCg");
        }

        [Test]
        public void NodeNullInRule()
        {
            var phono = CreateStringWithStdImports(
                    @"rule coronal-null [Coronal] => [*Place] / _ ");
            RunPhonixFile(phono, "fTx", "fhx");
        }

        [Test]
        public void LeftwardInsert()
        {
            var phono = CreateStringWithStdImports("rule leftward-insert (direction=right-to-left) * => c / b _ b");
            RunPhonixFile(phono, "abba", "abcba");
        }

        [Test]
        public void RightwardInsert()
        {
            var phono = CreateStringWithStdImports("rule rightward-insert (direction=left-to-right) * => c / b _ b");
            RunPhonixFile(phono, "abba", "abcba");
        }

        [Test]
        public void BasicExclude()
        {
            var phono = CreateStringWithStdImports("rule ex a => b / _ c // _ cc");
            RunPhonixFile(phono, "ac", "bc");
            RunPhonixFile(phono, "acc", "acc");
        }

        [Test]
        public void ExcludeContextLonger()
        {
            var phono = CreateStringWithStdImports("rule ex a => b / c[-vc] _  // c _");
            RunPhonixFile(phono, "csa", "csb");
            RunPhonixFile(phono, "cca", "cca");
        }

        [Test]
        public void ExcludeContextShorter()
        {
            var phono = CreateStringWithStdImports("rule ex a => b / k _  // sk _");
            RunPhonixFile(phono, "ka", "kb");
            RunPhonixFile(phono, "ska", "ska");
        }

        [Test]
        public void ExcludeNoContext()
        {
            var phono = CreateStringWithStdImports("rule ex a => b // c _");
            RunPhonixFile(phono, "ka", "kb");
            RunPhonixFile(phono, "ca", "ca");
        }

        [Test]
        public void ContextTrailingSlash()
        {
            var phono = CreateStringWithStdImports("rule ex a => b / _ c / ");
            RunPhonixFile(phono, "aac", "abc");
        }

        [Test]
        public void ExcludeSingleSlash()
        {
            var phono = CreateStringWithStdImports("rule ex a => b / _ c / a _ ");
            RunPhonixFile(phono, "aac", "aac");
        }

        [Test]
        public void Insert()
        {
            // middle
            var phono = CreateStringWithStdImports("rule insert * => a / b _ b");
            RunPhonixFile(phono, "bb", "bab");

            // beginning
            phono = CreateStringWithStdImports("rule insert * => a / _ bb");
            RunPhonixFile(phono, "bb", "abb");

            // end
            phono = CreateStringWithStdImports("rule insert * => a / bb _");
            RunPhonixFile(phono, "bb", "bba");
        }

        [Test]
        public void Delete()
        {
            // middle
            var phono = CreateStringWithStdImports("rule delete-middle a => * / b _ b");
            RunPhonixFile(phono, "bab", "bb");

            // beginning
            phono = CreateStringWithStdImports("rule delete-beginning a => * / _ bb");
            RunPhonixFile(phono, "abb", "bb");

            // end
            phono = CreateStringWithStdImports("rule delete-end a => * / bb _");
            RunPhonixFile(phono, "bba", "bb");

            // two
            phono = CreateStringWithStdImports("rule delete-double aa => **");
            RunPhonixFile(phono, "baab", "bb");
        }

        [Test]
        public void InsertDeleteInvalid()
        {
            /* TODO
            try
            {
                CreateStringWithStdImports("rule insert-delete * a => b * ");
                Assert.Fail("should have thrown exception");
            }
            catch (ParseException) {}
            try
            {
                CreateStringWithStdImports("rule delete-insert a * => * b ");
                Assert.Fail("should have thrown exception");
            }
            catch (ParseException) {}
            try
            {
                CreateStringWithStdImports("rule delete-insert a * => b * ");
                Assert.Fail("should have thrown exception");
            }
            catch (ParseException) {}
            */
        }

        [Test]
        public void RulePersist()
        {
            var phono = CreateStringWithStdImports("rule persist-b-a (persist) b => a   rule a-b a => b");
            RunPhonixFile(phono, "baa", "aaa");
        }

        [Test]
        public void SymbolDiacritic()
        {
            /* TODO
            var phono = CreateStringWithStdImports("symbol ~ (diacritic) [+nas]");
            Assert.AreEqual(1, phono.SymbolSet.Diacritics.Count);
            Assert.IsTrue(phono.SymbolSet.Diacritics.ContainsKey("~"));
            */
        }

        [Test]
        public void SegmentRepeatZeroOrMore()
        {
            var phono = CreateStringWithStdImports("rule matchany a => c / _ (b)*$ ");
            RunPhonixFile(phono, "a", "c");
            RunPhonixFile(phono, "ab", "cb");
            RunPhonixFile(phono, "abb", "cbb");
            RunPhonixFile(phono, "ac", "ac");
        }

        [Test]
        public void SegmentRepeatOneOrMore()
        {
            var phono = CreateStringWithStdImports("rule matchany a => c / _ (b)+$ ");
            RunPhonixFile(phono, "a", "a");
            RunPhonixFile(phono, "ab", "cb");
            RunPhonixFile(phono, "abb", "cbb");
            RunPhonixFile(phono, "ac", "ac");
        }

        [Test]
        public void SegmentRepeatZeroOrOne()
        {
            var phono = CreateStringWithStdImports("rule matchany a => c / _ (b)$ ");
            RunPhonixFile(phono, "a", "c");
            RunPhonixFile(phono, "ab", "cb");
            RunPhonixFile(phono, "abb", "abb");
            RunPhonixFile(phono, "ac", "ac");
        }

        [Test]
        public void MultipleSegmentOptional()
        {
            var phono = CreateStringWithStdImports("rule matchany a => c / _ (bc)c$ ");
            RunPhonixFile(phono, "a", "a");
            RunPhonixFile(phono, "ac", "cc");
            RunPhonixFile(phono, "abc", "abc");
            RunPhonixFile(phono, "abcc", "cbcc");
        }

        [Test]
        public void SegmentOptional()
        {
            var phono = CreateStringWithStdImports("rule matchany a => c / _ (b)+c$ ");
            RunPhonixFile(phono, "a", "a");
            RunPhonixFile(phono, "ac", "ac");
            RunPhonixFile(phono, "abc", "cbc");
            RunPhonixFile(phono, "abbc", "cbbc");
        }

        [Test]
        public void MultipleSegmentOptionalBacktrack()
        {
            var phono = CreateStringWithStdImports("rule matchany a => c / _ (bc)+b$ ");
            RunPhonixFile(phono, "abc", "abc");
            RunPhonixFile(phono, "abcb", "cbcb");
            RunPhonixFile(phono, "abcbc", "abcbc");
            RunPhonixFile(phono, "abcbcb", "cbcbcb");
        }

        [Test]
        public void SegmentRepeatZeroOrOnePre()
        {
            var phono = CreateStringWithStdImports("rule matchany a => x / (c)b _ $ ");
            RunPhonixFile(phono, "cba", "cbx");
            RunPhonixFile(phono, "ba", "bx");
            RunPhonixFile(phono, "ca", "ca");
        }

        [Test]
        public void SegmentRepeatZeroOnMorePre()
        {
            var phono = CreateStringWithStdImports("rule matchany a => x / (c)*b _ $ ");
            RunPhonixFile(phono, "cba", "cbx");
            RunPhonixFile(phono, "ccba", "ccbx");
            RunPhonixFile(phono, "cccba", "cccbx");
            RunPhonixFile(phono, "ba", "bx");
            RunPhonixFile(phono, "ca", "ca");
        }

        [Test]
        public void SegmentRepeatOneOrMorePre()
        {
            var phono = CreateStringWithStdImports("rule matchany a => x / (c)+b _ $ ");
            RunPhonixFile(phono, "cba", "cbx");
            RunPhonixFile(phono, "ccba", "ccbx");
            RunPhonixFile(phono, "cccba", "cccbx");
            RunPhonixFile(phono, "ba", "ba");
            RunPhonixFile(phono, "ca", "ca");
        }

        [Test]
        public void RuleApplicationRate()
        {
            /* TODO
            var phono = CreateStringWithStdImports("rule sporadic (applicationRate=0.25) a => b");
            var rule = phono.RuleSet.OrderedRules.First();
            Assert.AreEqual(0.25, ((Rule) rule).ApplicationRate);
            */
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void RuleApplicationRateOutOfRange()
        {
            CreateStringWithStdImports("rule sporadic (applicationRate=1.25) a => b");
            Assert.Fail("Shouldn't reach this line");
        }

        [Test]
        public void ScalarRange()
        {
            /* TODO
            var phono = CreateStringWithStdImports("feature scRange (type=scalar min=1 max=4)");
            Assert.IsTrue(phono.FeatureSet.Has<ScalarFeature>("scRange"));

            var sc = phono.FeatureSet.Get<ScalarFeature>("scRange");
            Assert.AreEqual(1, sc.Min.Value);
            Assert.AreEqual(4, sc.Max.Value);
            */
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void ScalarMissingMin()
        {
            CreateStringWithStdImports("feature scRange (type=scalar max=4)");
            Assert.Fail("Shouldn't reach this line");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void ScalarMissingMax()
        {
            CreateStringWithStdImports("feature scRange (type=scalar min=1)");
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
            string phono = scalarDefs + "rule neq [sc<>0] => sc2";
            RunPhonixFile(phono, "sc0", "sc0");
            RunPhonixFile(phono, "sc1", "sc2");
            RunPhonixFile(phono, "sc2", "sc2");
            RunPhonixFile(phono, "sc3", "sc2");
            RunPhonixFile(phono, "scX", "sc2");
        }

        [Test]
        public void ScalarGT()
        {
            string phono = scalarDefs + "rule gt [sc>1] => sc0";
            RunPhonixFile(phono, "sc0", "sc0");
            RunPhonixFile(phono, "sc1", "sc1");
            RunPhonixFile(phono, "sc2", "sc0");
            RunPhonixFile(phono, "sc3", "sc0");
            RunPhonixFile(phono, "scX", "scX");
        }

        [Test]
        public void ScalarGTOrEq()
        {
            string phono = scalarDefs + "rule gte [sc>=2] => sc0";
            RunPhonixFile(phono, "sc0", "sc0");
            RunPhonixFile(phono, "sc1", "sc1");
            RunPhonixFile(phono, "sc2", "sc0");
            RunPhonixFile(phono, "sc3", "sc0");
            RunPhonixFile(phono, "scX", "scX");
        }

        [Test]
        public void ScalarLT()
        {
            string phono = scalarDefs + "rule lt [sc<2] => sc3";
            RunPhonixFile(phono, "sc0", "sc3");
            RunPhonixFile(phono, "sc1", "sc3");
            RunPhonixFile(phono, "sc2", "sc2");
            RunPhonixFile(phono, "sc3", "sc3");
            RunPhonixFile(phono, "scX", "scX");
        }

        [Test]
        public void ScalarLTOrEq()
        {
            string phono = scalarDefs + "rule lte [sc<=1] => sc3";
            RunPhonixFile(phono, "sc0", "sc3");
            RunPhonixFile(phono, "sc1", "sc3");
            RunPhonixFile(phono, "sc2", "sc2");
            RunPhonixFile(phono, "sc3", "sc3");
            RunPhonixFile(phono, "scX", "scX");
        }

        [Test]
        public void ScalarAdd()
        {
            string phono = scalarDefs + "rule add [sc<3] => [sc=+1]";
            RunPhonixFile(phono, "sc0", "sc1");
            RunPhonixFile(phono, "sc1", "sc2");
            RunPhonixFile(phono, "sc2", "sc3");
            RunPhonixFile(phono, "sc3", "sc3");
            RunPhonixFile(phono, "scX", "scX");
        }

        [Test]
        public void ScalarSubtract()
        {
            string phono = scalarDefs + "rule subtract [sc>0] => [sc=-1]";
            RunPhonixFile(phono, "sc0", "sc0");
            RunPhonixFile(phono, "sc1", "sc0");
            RunPhonixFile(phono, "sc2", "sc1");
            RunPhonixFile(phono, "sc3", "sc2");
            RunPhonixFile(phono, "scX", "scX");
        }

        [Test]
        public void ScalarValueOutOfRange()
        {
            /* TODO
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

            RunPhonixFile(phono, "sc0", "sc0"); // assert that this doesn't blow up

            Assert.IsTrue(gotTrace);
            Assert.AreSame(rule, phono.RuleSet.OrderedRules.Where(r => r.Name.Equals("subtract")).First());
            Assert.AreSame(feature, phono.FeatureSet.Get<Feature>("sc"));
            Assert.AreEqual(-1, traceValue);
            */
        }

        [Test]
        public void ScalarValueInvalidOp()
        {
            /* TODO
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

            RunPhonixFile(phono, "scX", "scX"); // this shouldn't throw an exception

            Assert.IsTrue(gotTrace);
            Assert.AreSame(rule, phono.RuleSet.OrderedRules.Where(r => r.Name.Equals("subtract")).First());
            Assert.AreSame(feature, phono.FeatureSet.Get<Feature>("sc"));
            */
        }

        [Test]
        public void SyllableCV()
        {
            var phono = CreateStringWithStdImports("syllable onset [+cons] nucleus [-cons +son]");
            ApplySyllableRule(phono, "basiho", "<b:a><s:i><h:o>");
            ApplySyllableRule(phono, "asiho", "<a><s:i><h:o>");
            ApplySyllableRule(phono, "basih", "<b:a><s:i>h");
        }

        [Test]
        public void SyllableCVC()
        {
            var phono = CreateStringWithStdImports("syllable onset [+cons] nucleus [-cons +son] coda [+son]");
            ApplySyllableRule(phono, "basihon", "<b:a><s:i><h:o.n>");
            ApplySyllableRule(phono, "asihon", "<a><s:i><h:o.n>");
            ApplySyllableRule(phono, "basih", "<b:a><s:i>h");
            ApplySyllableRule(phono, "bai", "<b:a.i>");
        }

        [Test]
        public void SyllableCVCOnsetRequired()
        {
            var phono = CreateStringWithStdImports("syllable (onsetRequired) onset [+cons] nucleus [-cons +son] coda [+son]");
            ApplySyllableRule(phono, "basihon", "<b:a><s:i><h:o.n>");
            ApplySyllableRule(phono, "asihon", "a<s:i><h:o.n>");
            ApplySyllableRule(phono, "basih", "<b:a><s:i>h");
            ApplySyllableRule(phono, "bai", "<b:a.i>");
        }

        [Test]
        public void SyllableCVCCodaRequired()
        {
            var phono = CreateStringWithStdImports("syllable (codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons]");
            ApplySyllableRule(phono, "basihon", "<b:a.s><i.h><o.n>");
            ApplySyllableRule(phono, "asihon", "<a.s><i.h><o.n>");
            ApplySyllableRule(phono, "basih", "<b:a.s><i.h>");
            ApplySyllableRule(phono, "bai", "bai");
        }

        [Test]
        public void SyllableCVCOnsetAndCodaRequired()
        {
            var phono = CreateStringWithStdImports(
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
            var phono = CreateStringWithStdImports("syllable onset [+cons]([+cons]) nucleus [-cons +son]");
            ApplySyllableRule(phono, "basiho", "<b:a><s:i><h:o>");
            ApplySyllableRule(phono, "brastihno", "<br:a><st:i><hn:o>");
            ApplySyllableRule(phono, "aszihxon", "<a><sz:i><hx:o>n");
            ApplySyllableRule(phono, "barsih", "<b:a><rs:i>h");
        }

        [Test]
        public void SyllableCCVC()
        {
            var phono = CreateStringWithStdImports(
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
            var phono = CreateStringWithStdImports(
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
            var phono = CreateStringWithStdImports(
                    "syllable (nucleusPreference=right) " + 
                    "onset [+cons]([+son]) nucleus [-cons +son] coda []");
            ApplySyllableRule(phono, "bui", "<bu:i>");
            ApplySyllableRule(phono, "biu", "<bi:u>");
        }

        [Test]
        public void SyllableCVCNucleusLeft()
        {
            var phono = CreateStringWithStdImports(
                    "syllable (nucleusPreference=left) " + 
                    "onset [+cons][+son] onset [+cons] nucleus [-cons +son] coda []");
            ApplySyllableRule(phono, "bui", "<b:u.i>");
            ApplySyllableRule(phono, "biu", "<b:i.u>");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SyllableInvalidNoSegmentInOnset()
        {
            CreateStringWithStdImports("syllable onset nucleus [] coda []");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SyllableInvalidNoSegmentInNucleus()
        {
            CreateStringWithStdImports("syllable onset [] nucleus coda []");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SyllableInvalidNoSegmentInCoda()
        {
            CreateStringWithStdImports("syllable onset [] nucleus [] coda ");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SyllableInvalidRegexPlus()
        {
            CreateStringWithStdImports("syllable onset ([])+ nucleus [] coda []");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SyllableInvalidRegexStar()
        {
            CreateStringWithStdImports("syllable onset ([])* nucleus [] coda []");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SyllableInvalidParameter()
        {
            CreateStringWithStdImports("syllable (invalid) onset [] nucleus [] coda []");
        }

        [Test]
        public void MatchOnset()
        {
            var phono = CreateStringWithStdImports(
                    "syllable onset [+cons]([+cons]) nucleus [-cons +son] coda [+cons] " +
                    "rule markOnset [<onset>] => x / _ [<*onset>]");
            RunPhonixFile(phono, "basiho", "xaxixo");
            RunPhonixFile(phono, "brastihno", "bxasxihxo");
            RunPhonixFile(phono, "aszihgon", "asxihxon");
            RunPhonixFile(phono, "barsih", "xarxih");
        }

        [Test]
        public void MatchCoda()
        {
            var phono = CreateStringWithStdImports(
                    "syllable onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markCoda [<coda>] => x / [<*coda>] _ ");
            RunPhonixFile(phono, "basiho", "basiho");
            RunPhonixFile(phono, "brastihno", "braxtixno");
            RunPhonixFile(phono, "aszihgon", "axzixgox");
            RunPhonixFile(phono, "barsih", "baxsix");
        }

        [Test]
        public void MatchNucleus()
        {
            var phono = CreateStringWithStdImports(
                    "syllable onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markCoda [<nucleus>] => x ");
            RunPhonixFile(phono, "basiho", "bxsxhx");
            RunPhonixFile(phono, "brastihno", "brxstxhnx");
            RunPhonixFile(phono, "aszihgon", "xszxhgxn");
            RunPhonixFile(phono, "barsih", "bxrsxh");
        }

        [Test]
        public void MatchSyllable()
        {
            var phono = CreateStringWithStdImports(
                    "syllable (onsetRequired codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markSyllable [<syllable>] => x ");
            RunPhonixFile(phono, "basiho", "xxxiho");
            RunPhonixFile(phono, "brastihno", "bxxxxxxno");
            RunPhonixFile(phono, "aszihgon", "asxxxxxx");
            RunPhonixFile(phono, "barsih", "xxxxxx");
        }

        [Test]
        public void MatchNoSyllable()
        {
            var phono = CreateStringWithStdImports(
                    "syllable (onsetRequired codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markNoSyllable [<*syllable>] => x ");
            RunPhonixFile(phono, "basiho", "basxxx");
            RunPhonixFile(phono, "brastihno", "xrastihxx");
            RunPhonixFile(phono, "aszihgon", "xxzihgon");
            RunPhonixFile(phono, "barsih", "barsih");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void UnmatchedLeftBracket()
        {
            CreateStringWithStdImports("rule markInvalid [<syllable] => x");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void UnmatchedRightBracket()
        {
            CreateStringWithStdImports("rule markInvalid [syllable>] => x");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void UnrecognizedTierName()
        {
            CreateStringWithStdImports("rule markInvalid [<wrong>] => x");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SymbolContainsVariable()
        {
            CreateStringWithStdImports("symbol ! [+hi -lo $vc]");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SymbolContainsScalarOp()
        {
            CreateStringWithStdImports("feature sc (type=scalar)   symbol ! [+hi -lo sc=+1]");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SymbolContainsBareNode()
        {
            CreateStringWithStdImports("symbol ! [+hi -lo Labial]");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SymbolContainsNullNode()
        {
            CreateStringWithStdImports("symbol ! [+hi -lo *Labial]");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void SymbolContainsSyllableFeature()
        {
            CreateStringWithStdImports("symbol ! [+vc <coda>]");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void RuleMatchContainsScalarOp()
        {
            CreateStringWithStdImports("feature sc (type=scalar)   rule ! [sc=+1] => []");
        }

        [Test]
        //[ExpectedException(typeof(ParseException))]
        public void RuleActionContainsBareNode()
        {
            CreateStringWithStdImports("rule ! [] => [Labial]");
        }

        [Test]
        public void FeatureGroupDeclaration()
        {
            CreateStringWithStdImports("feature [V] [+syll +son +vc]");
        }

        [Test]
        public void SymbolContainsFeatureGroup()
        {
            CreateStringWithStdImports("feature [V] [+syll +son +vc]   symbol ! [[V] -hi]");
        }

        [Test]
        public void RuleMatchContainsFeatureGroup()
        {
            var phono = CreateStringWithStdImports("feature [V] [+syll +son]\n" +
                                            "rule match-group [[V]] => i");
            RunPhonixFile(phono, "bomo", "bimi");
        }

        [Test]
        public void RuleActionContainsFeatureGroup()
        {
            var phono = CreateStringWithStdImports("feature [V] [+syll +son]\n" +
                                            "rule match-group [+syll -hi] => [[V] +hi -lo +fr -bk]");
            RunPhonixFile(phono, "bomo", "bymy");
        }

        [Test]
        public void SymbolContainsSymbolRef()
        {
            CreateStringWithStdImports("symbol s! [(s) -cons]");
        }

        [Test]
        public void RuleMatchContainsSymbolRef()
        {
            var phono = CreateStringWithStdImports("import std.symbols.diacritics\nrule match-sym [(a)] => i");
            RunPhonixFile(phono, "ba~ma_0", "bimi");
        }

        [Test]
        public void RuleMatchAndActionContainSymbolRef()
        {
            var phono = CreateStringWithStdImports("import std.symbols.diacritics\nrule match-sym [(a)] => [(i)]");
            RunPhonixFile(phono, "ba~ma_0", "bi~mi_0");
        }

        [Test]
        public void StringQuoting()
        {
            CreateStringWithStdImports(
			"symbol \"quote\" [+cons -ro]" +
			"symbol \"quote with space\" [+cons -ro]" +
			"symbol \"quote +(with) -<bad> $[chars]\" [+cons -ro]" +
			"symbol \"quote isn't awesome\" [+cons -ro]" +
			"symbol 'quote' [+cons -ro]" +
			"symbol 'quote with space' [+cons -ro]" +
			"symbol 'quote +(with) -<bad> $[chars]' [+cons -ro]" +
			"symbol 'quote has \" in it' [+cons -ro]"
			);
        }
    }
}
