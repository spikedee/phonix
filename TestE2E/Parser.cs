using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Phonix.TestE2E
{
    using NUnit.Framework;

    [TestFixture]
    public class ParserTest
    {
        [Test]
        public void RuleWithVariable()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule voice-assimilate [] => [$vc] / _ [$vc]");
            phono.Start().ValidateInOut("sz", "zz");
            phono.End();
        }

        [Test]
        public void RuleDirectionRightward()
        {
            // default direction should be rightward
            var phono = new PhonixWrapper().StdImports().Append("rule rightward a => b / a _");
            phono.Start().ValidateInOut("aaa", "aba");
            phono.End();

            var phono2 = new PhonixWrapper().StdImports().Append("rule rightward (direction=left-to-right) a => b / a _");
            phono2.Start().ValidateInOut("aaa", "aba");
            phono2.End();
        }

        [Test]
        public void RuleDirectionLeftward()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule leftward (direction=right-to-left) a => b / a _");
            phono.Start().ValidateInOut("aaa", "abb");
            phono.End();
        }

        [Test]
        public void NodeExistsInRule()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    @"rule coronal-test [Coronal] => [+vc]");
            phono.Start().ValidateInOut("ptk", "pdk");
            phono.End();
        }

        [Test]
        public void NodeVariableInRule()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    @"rule coronal-test [] => [$Coronal] / _ [$Coronal -vc]");
            phono.Start().ValidateInOut("TCg", "CCg");
            phono.End();
        }

        [Test]
        public void NodeNullInRule()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    @"rule coronal-null [Coronal] => [*Place] / _ ");
            phono.Start().ValidateInOut("fTx", "fhx");
            phono.End();
        }

        [Test]
        public void LeftwardInsert()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule leftward-insert (direction=right-to-left) * => c / b _ b");
            phono.Start().ValidateInOut("abba", "abcba");
            phono.End();
        }

        [Test]
        public void RightwardInsert()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule rightward-insert (direction=left-to-right) * => c / b _ b");
            phono.Start().ValidateInOut("abba", "abcba");
            phono.End();
        }

        [Test]
        public void BasicExclude()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule ex a => b / _ c // _ cc");
            phono.Start()
                .ValidateInOut("ac", "bc")
                .ValidateInOut("acc", "acc")
                .End();
        }

        [Test]
        public void ExcludeContextLonger()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule ex a => b / c[-vc] _  // c _");
            phono.Start()
                .ValidateInOut("csa", "csb")
                .ValidateInOut("cca", "cca")
                .End();
        }

        [Test]
        public void ExcludeContextShorter()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule ex a => b / k _  // sk _");
            phono.Start()
                .ValidateInOut("ka", "kb")
                .ValidateInOut("ska", "ska")
                .End();
        }

        [Test]
        public void ExcludeNoContext()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule ex a => b // c _");
            phono.Start()
                .ValidateInOut("ka", "kb")
                .ValidateInOut("ca", "ca")
                .End();
        }

        [Test]
        public void ContextTrailingSlash()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule ex a => b / _ c / ");
            phono.Start().ValidateInOut("aac", "abc");
            phono.End();
        }

        [Test]
        public void ExcludeSingleSlash()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule ex a => b / _ c / a _ ");
            phono.Start().ValidateInOut("aac", "aac");
            phono.End();
        }

        [Test]
        public void Insert()
        {
            // middle
            var phono = new PhonixWrapper().StdImports().Append("rule insert * => a / b _ b");
            phono.Start().ValidateInOut("bb", "bab");
            phono.End();

            // beginning
            phono = new PhonixWrapper().StdImports().Append("rule insert * => a / _ bb");
            phono.Start().ValidateInOut("bb", "abb");
            phono.End();

            // end
            phono = new PhonixWrapper().StdImports().Append("rule insert * => a / bb _");
            phono.Start().ValidateInOut("bb", "bba");
            phono.End();
        }

        [Test]
        public void Delete()
        {
            // middle
            var phono = new PhonixWrapper().StdImports().Append("rule delete-middle a => * / b _ b");
            phono.Start().ValidateInOut("bab", "bb");
            phono.End();

            // beginning
            phono = new PhonixWrapper().StdImports().Append("rule delete-beginning a => * / _ bb");
            phono.Start().ValidateInOut("abb", "bb");
            phono.End();

            // end
            phono = new PhonixWrapper().StdImports().Append("rule delete-end a => * / bb _");
            phono.Start().ValidateInOut("bba", "bb");
            phono.End();

            // two
            phono = new PhonixWrapper().StdImports().Append("rule delete-double aa => **");
            phono.Start().ValidateInOut("baab", "bb");
            phono.End();
        }

        [Test]
        public void RulePersist()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule persist-b-a (persist) b => a   rule a-b a => b");
            phono.Start().ValidateInOut("baa", "aaa");
            phono.End();
        }

        [Test]
        public void SegmentRepeatZeroOrMore()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => c / _ (b)*$ ");
            phono.Start()
                .ValidateInOut("a", "c")
                .ValidateInOut("ab", "cb")
                .ValidateInOut("abb", "cbb")
                .ValidateInOut("ac", "ac")
                .End();
        }

        [Test]
        public void SegmentRepeatOneOrMore()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => c / _ (b)+$ ");
            phono.Start().ValidateInOut("a", "a");
            phono.ValidateInOut("ab", "cb");
            phono.ValidateInOut("abb", "cbb");
            phono.ValidateInOut("ac", "ac");
            phono.End();
        }

        [Test]
        public void SegmentRepeatZeroOrOne()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => c / _ (b)$ ");
            phono.Start().ValidateInOut("a", "c");
            phono.ValidateInOut("ab", "cb");
            phono.ValidateInOut("abb", "abb");
            phono.ValidateInOut("ac", "ac");
            phono.End();
        }

        [Test]
        public void MultipleSegmentOptional()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => c / _ (bc)c$ ");
            phono.Start();
            phono.ValidateInOut("a", "a");
            phono.ValidateInOut("ac", "cc");
            phono.ValidateInOut("abc", "abc");
            phono.ValidateInOut("abcc", "cbcc");
            phono.End();
        }

        [Test]
        public void SegmentOptional()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => c / _ (b)+c$ ");
            phono.Start().ValidateInOut("a", "a");
            phono.ValidateInOut("ac", "ac");
            phono.ValidateInOut("abc", "cbc");
            phono.ValidateInOut("abbc", "cbbc");
            phono.End();
        }

        [Test]
        public void MultipleSegmentOptionalBacktrack()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => c / _ (bc)+b$ ");
            phono.Start().ValidateInOut("abc", "abc");
            phono.ValidateInOut("abcb", "cbcb");
            phono.ValidateInOut("abcbc", "abcbc");
            phono.ValidateInOut("abcbcb", "cbcbcb");
            phono.End();
        }

        [Test]
        public void SegmentRepeatZeroOrOnePre()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => x / (c)b _ $ ");
            phono.Start().ValidateInOut("cba", "cbx");
            phono.ValidateInOut("ba", "bx");
            phono.ValidateInOut("ca", "ca");
            phono.End();
        }

        [Test]
        public void SegmentRepeatZeroOnMorePre()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => x / (c)*b _ $ ");
            phono.Start().ValidateInOut("cba", "cbx");
            phono.ValidateInOut("ccba", "ccbx");
            phono.ValidateInOut("cccba", "cccbx");
            phono.ValidateInOut("ba", "bx");
            phono.ValidateInOut("ca", "ca");
            phono.End();
        }

        [Test]
        public void SegmentRepeatOneOrMorePre()
        {
            var phono = new PhonixWrapper().StdImports().Append("rule matchany a => x / (c)+b _ $ ");
            phono.Start().ValidateInOut("cba", "cbx");
            phono.ValidateInOut("ccba", "ccbx");
            phono.ValidateInOut("cccba", "cccbx");
            phono.ValidateInOut("ba", "ba");
            phono.ValidateInOut("ca", "ca");
            phono.End();
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
            var phono = new PhonixWrapper().Append(scalarDefs).Append("rule neq [sc<>0] => sc2");
            phono.Start().ValidateInOut("sc0", "sc0");
            phono.ValidateInOut("sc1", "sc2");
            phono.ValidateInOut("sc2", "sc2");
            phono.ValidateInOut("sc3", "sc2");
            phono.ValidateInOut("scX", "sc2");
            phono.End();
        }

        [Test]
        public void ScalarGT()
        {
            var phono = new PhonixWrapper().Append(scalarDefs).Append("rule gt [sc>1] => sc0");
            phono.Start().ValidateInOut("sc0", "sc0");
            phono.ValidateInOut("sc1", "sc1");
            phono.ValidateInOut("sc2", "sc0");
            phono.ValidateInOut("sc3", "sc0");
            phono.ValidateInOut("scX", "scX");
            phono.End();
        }

        [Test]
        public void ScalarGTOrEq()
        {
            var phono = new PhonixWrapper().Append(scalarDefs + "rule gte [sc>=2] => sc0");
            phono.Start().ValidateInOut("sc0", "sc0");
            phono.ValidateInOut("sc1", "sc1");
            phono.ValidateInOut("sc2", "sc0");
            phono.ValidateInOut("sc3", "sc0");
            phono.ValidateInOut("scX", "scX");
            phono.End();
        }

        [Test]
        public void ScalarLT()
        {
            var phono = new PhonixWrapper().Append(scalarDefs + "rule lt [sc<2] => sc3");
            phono.Start().ValidateInOut("sc0", "sc3");
            phono.ValidateInOut("sc1", "sc3");
            phono.ValidateInOut("sc2", "sc2");
            phono.ValidateInOut("sc3", "sc3");
            phono.ValidateInOut("scX", "scX");
            phono.End();
        }

        [Test]
        public void ScalarLTOrEq()
        {
            var phono = new PhonixWrapper().Append(scalarDefs + "rule lte [sc<=1] => sc3");
            phono.Start().ValidateInOut("sc0", "sc3");
            phono.ValidateInOut("sc1", "sc3");
            phono.ValidateInOut("sc2", "sc2");
            phono.ValidateInOut("sc3", "sc3");
            phono.ValidateInOut("scX", "scX");
            phono.End();
        }

        [Test]
        public void ScalarAdd()
        {
            var phono = new PhonixWrapper().Append(scalarDefs + "rule add [sc<3] => [sc=+1]");
            phono.Start().ValidateInOut("sc0", "sc1");
            phono.ValidateInOut("sc1", "sc2");
            phono.ValidateInOut("sc2", "sc3");
            phono.ValidateInOut("sc3", "sc3");
            phono.ValidateInOut("scX", "scX");
            phono.End();
        }

        [Test]
        public void ScalarSubtract()
        {
            var phono = new PhonixWrapper().Append(scalarDefs + "rule subtract [sc>0] => [sc=-1]");
            phono.Start().ValidateInOut("sc0", "sc0");
            phono.ValidateInOut("sc1", "sc0");
            phono.ValidateInOut("sc2", "sc1");
            phono.ValidateInOut("sc3", "sc2");
            phono.ValidateInOut("scX", "scX");
            phono.End();
        }

        [Test]
        public void MatchOnset()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable onset [+cons]([+cons]) nucleus [-cons +son] coda [+cons] " +
                    "rule markOnset [<onset>] => x / _ [<*onset>]");
            phono.Start().ValidateInOut("basiho", "xaxixo");
            phono.ValidateInOut("brastihno", "bxasxihxo");
            phono.ValidateInOut("aszihgon", "asxihxon");
            phono.ValidateInOut("barsih", "xarxih");
            phono.End();
        }

        [Test]
        public void MatchCoda()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markCoda [<coda>] => x / [<*coda>] _ ");
            phono.Start();
            phono.ValidateInOut("basiho", "basiho");
            phono.ValidateInOut("brastihno", "braxtixno");
            phono.ValidateInOut("aszihgon", "axzixgox");
            phono.ValidateInOut("barsih", "baxsix");
            phono.End();
        }

        [Test]
        public void MatchNucleus()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markCoda [<nucleus>] => x ");
            phono.Start().ValidateInOut("basiho", "bxsxhx");
            phono.ValidateInOut("brastihno", "brxstxhnx");
            phono.ValidateInOut("aszihgon", "xszxhgxn");
            phono.ValidateInOut("barsih", "bxrsxh");
            phono.End();
        }

        [Test]
        public void MatchSyllable()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable (onsetRequired codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markSyllable [<syllable>] => x ");
            phono.Start().ValidateInOut("basiho", "xxxiho");
            phono.ValidateInOut("brastihno", "bxxxxxxno");
            phono.ValidateInOut("aszihgon", "asxxxxxx");
            phono.ValidateInOut("barsih", "xxxxxx");
            phono.End();
        }

        [Test]
        public void MatchNoSyllable()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable (onsetRequired codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons] " +
                    "rule markNoSyllable [<*syllable>] => x ");
            phono.Start().ValidateInOut("basiho", "basxxx");
            phono.ValidateInOut("brastihno", "xrastihxx");
            phono.ValidateInOut("aszihgon", "xxzihgon");
            phono.ValidateInOut("barsih", "barsih");
            phono.End();
        }

        [Test]
        public void FeatureGroupDeclaration()
        {
            new PhonixWrapper().StdImports().Append("feature [V] [+syll +son +vc]");
        }

        [Test]
        public void SymbolContainsFeatureGroup()
        {
            new PhonixWrapper().StdImports().Append("feature [V] [+syll +son +vc]   symbol ! [[V] -hi]");
        }

        [Test]
        public void RuleMatchContainsFeatureGroup()
        {
            var phono = new PhonixWrapper().StdImports().Append("feature [V] [+syll +son]\n" +
                                            "rule match-group [[V]] => i");
            phono.Start().ValidateInOut("bomo", "bimi");
            phono.End();
        }

        [Test]
        public void RuleActionContainsFeatureGroup()
        {
            var phono = new PhonixWrapper().StdImports().Append("feature [V] [+syll +son]\n" +
                                            "rule match-group [+syll -hi] => [[V] +hi -lo +fr -bk]");
            phono.Start().ValidateInOut("bomo", "bymy");
            phono.End();
        }

        [Test]
        public void SymbolContainsSymbolRef()
        {
            new PhonixWrapper().StdImports().Append("symbol s! [(s) -cons]");
        }

        [Test]
        public void RuleMatchContainsSymbolRef()
        {
            var phono = new PhonixWrapper().StdImports().Append("import std.symbols.diacritics\nrule match-sym [(a)] => i");
            phono.Start().ValidateInOut("ba~ma_0", "bimi");
            phono.End();
        }

        [Test]
        public void RuleMatchAndActionContainSymbolRef()
        {
            var phono = new PhonixWrapper().StdImports().Append("import std.symbols.diacritics\nrule match-sym [(a)] => [(i)]");
            phono.Start().ValidateInOut("ba~ma_0", "bi~mi_0");
            phono.End();
        }

        [Test]
        public void StringQuoting()
        {
            var phono = new PhonixWrapper().StdImports().Append(
			"symbol \"quote\" [-cons -ro]" +
			"symbol \"quote with space\" [+cons -ro]" +
			"symbol \"quote +(with) -<bad> $[chars]\" [+cons +ro]" +
			"symbol \"quote isn't awesome\" [-cons +ro]" +
			"symbol 'squote' [-cons -str]" +
			"symbol 'squote with space' [+cons -str]" +
			"symbol 'squote +(with) -<bad> $[chars]' [+cons +str]" +
			"symbol 'squote has \" in it' [-cons +str]"
			);
            phono.Start().End();
        }
    }
}
