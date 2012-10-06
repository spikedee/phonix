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
    public class ErrorTest
    {
        [Test]
        public void RuleApplicationRateOutOfRange()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "rule sporadic (applicationRate=1.25) a => b", 
                        "Invalid value for applicationRate: 1.25 (value was not between 0 and 1)")
                .Start()
                .End();
        }

        [Test]
        public void ScalarMissingMin()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "feature scRange (type=scalar max=4)",
                        "Invalid value for min/max: both min and max required")
                .Start()
                .End();
        }

        [Test]
        public void ScalarMissingMax()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "feature scRange (type=scalar min=1)",
                        "Invalid value for min/max: both min and max required")
                .Start()
                .End();
        }

        [Test]
        public void SyllableInvalidNoSegmentInOnset()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "syllable onset nucleus [] coda []",
                        "Unexpected 'nucleus'")
                .Start()
                .End();
        }

        [Test]
        public void SyllableInvalidNoSegmentInNucleus()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "syllable onset [] nucleus coda []",
                        "Unexpected 'coda'")
                .Start()
                .End();
        }

        [Test]
        public void SyllableInvalidNoSegmentInCoda()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "syllable onset [] nucleus [] coda    feature foo",
                        "Unexpected 'feature'")
                .Start()
                .End();
        }

        [Test]
        public void SyllableInvalidRegexPlus()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "syllable onset ([])+ nucleus [] coda []",
                        "Unexpected '+'")
                .Start()
                .End();
        }

        [Test]
        public void SyllableInvalidRegexStar()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "syllable onset ([])* nucleus [] coda []",
                        "Unexpected '*'")
                .Start()
                .End();
        }

        [Test]
        public void SyllableInvalidParameter()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "syllable (invalid) onset [] nucleus [] coda []",
                        "Unknown parameter: 'invalid'")
                .Start()
                .End();
        }

        [Test]
        public void UnmatchedLeftBracket()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "rule markInvalid [<syllable] => x",
                        "Unexpected ']'")
                .Start()
                .End();
        }

        [Test]
        public void UnmatchedRightBracket()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "rule markInvalid [syllable>] => x",
                        "Unexpected 'syllable'",
                        "Unexpected '>'")
                .Start()
                .End();
        }

        [Test]
        public void UnrecognizedTierName()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "rule markInvalid [<wrong>] => x",
                        "No such tier: 'wrong'",
                        "Unexpected '>'",
                        "Unexpected '=>'")
                .Start()
                .End();
        }

        [Test]
        public void SymbolContainsVariable()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "symbol ! [+hi -lo $vc]",
                        "'$vc' cannot be used here; should be a concrete feature value")
                .Start()
                .End();
        }

        [Test]
        public void SymbolContainsScalarOp()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "feature sc (type=scalar)   symbol ! [+hi -lo sc=+1]",
                        "'sc=+1' cannot be used here; should be a concrete feature value")
                .Start()
                .End();
        }

        [Test]
        public void SymbolContainsBareNode()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "symbol ! [+hi -lo Labial]",
                        "'Labial' cannot be used here; should be a concrete feature value")
                .Start()
                .End();
        }

        [Test]
        public void SymbolContainsNullNode()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "symbol ! [+hi -lo *Labial]",
                        "'*Labial' cannot be used here; should be a concrete feature value")
                .Start()
                .End();
        }

        [Test]
        public void SymbolContainsSyllableFeature()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "symbol ! [+vc <coda>]",
                        "'<coda>' cannot be used here; should be a concrete feature value")
                .Start()
                .End();
        }

        [Test]
        public void RuleMatchContainsScalarOp()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "feature sc (type=scalar)   rule ! [sc=+1] => []",
                        "'sc=+1' cannot be used here; should be a concrete feature value, variable value, node value, or syllable feature",
                        "Unexpected '=>'")
                .Start()
                .End();
        }

        [Test]
        public void RuleActionContainsBareNode()
        {
            var phono = new PhonixWrapper();
            phono.StdImports()
                .AppendExpectError(
                        "rule ! [] => [Labial]",
                        "'Labial' cannot be used here; should be a concrete feature value or variable value")
                .Start()
                .End();
        }

        [Test]
        public void ImportedFileContainsError()
        {
            var importedFile = Path.GetTempFileName();
            File.WriteAllText(importedFile, "rule (noname) [] => []");

            var phono = new PhonixWrapper();
            phono.
                StdImports().
                Append(String.Format("import '{0}'", importedFile)).
                ExpectError(importedFile, 1, "Unexpected '('").
                Start().
                End();
        }

        [Test]
        public void RuleWithVariableUndefined()
        {
            var phono = new PhonixWrapper();
            phono.
                StdImports().
                Append("rule voice-assimilate [Labial] => [$vc] / _ []").
                Start().
                ValidateInOut("bai", "bai").
                ValidateWarning("In rule 'voice-assimilate': variable $vc used without appearing in rule context; some parts of this rule may be skipped").
                End();
        }

        [Test]
        public void InsertDeleteInvalid()
        {
            var phono = new PhonixWrapper();
            phono.
                StdImports().
                AppendExpectError(
                        "rule insert-delete * a => b * ",
                        "Can't insert and delete as part of the same rule").
                AppendExpectError(
                        "rule delete-insert a * => * b ",
                        "Can't insert and delete as part of the same rule").
                AppendExpectError(
                        "rule delete-insert a * => b * ",
                        "Can't map zero to zero").
                Start().
                End();
        }

        [Test]
        public void SymbolDiacritic()
        {
            var phono = new PhonixWrapper();
            phono.
                StdImports().
                Append("symbol ~ (diacritic) [+nas]").
                Append("rule denasalize [+nas] => [*nas]").
                Start().
                ValidateInOut("ba~", "ba").
                End();
        }

        [Test]
        public void ScalarValueLessThanZero()
        {
            var phono = new PhonixWrapper();
            phono.
                StdImports().
                Append("feature sc (type=scalar)").
                Append("symbol sc0 [sc=0]").
                Append("rule subtract [] => [sc=-1]").
                Start().
                ValidateInOut("sc0", "sc0").
                ValidateWarning("In rule 'subtract': resulting value sc=-1 is less than the minimum value 0; some parts of this rule may be skipped").
                End();
        }

        [Test]
        public void ScalarValueOutOfRange()
        {
            var phono = new PhonixWrapper();
            phono.
                StdImports().
                Append("feature sc (type=scalar min=1 max=3)").
                Append("symbol sc0 [sc=3]").
                Append("rule add [] => [sc=+1]").
                Start().
                ValidateInOut("sc0", "sc0").
                ValidateWarning("In rule 'add': resulting value sc=4 is greater than the maximum value 3; some parts of this rule may be skipped").
                End();
        }

        [Test]
        public void NoSuchFeatureValueGroup()
        {
            var phono = new PhonixWrapper();
            phono.
                StdImports().
                AppendExpectError(
                    "symbol sc0 [[mistake]]",
                    "Feature value group 'mistake' not found",
                    "Unexpected ']'").
                Start().
                End();
        }
    }
}
