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
    public class SyllableTest
    {
        [Test]
        public void SyllableCV()
        {
            var phono = new PhonixWrapper().StdImports().Append("syllable onset [+cons] nucleus [-cons +son]");
            phono.Start("-d");
            phono.ValidateSyllableRule("basiho", "<b :: a>  <s :: i>  <h :: o>");
            phono.ValidateSyllableRule("asiho", "<a>  <s :: i>  <h :: o>");
            phono.ValidateSyllableRule("basih", "<b :: a>  <s :: i> h");
            phono.End();
        }

        [Test]
        public void SyllableCVC()
        {
            var phono = new PhonixWrapper().StdImports().Append("syllable onset [+cons] nucleus [-cons +son] coda [+son]");
            phono.Start("-d");
            phono.ValidateSyllableRule("basihon", "<b :: a>  <s :: i>  <h :: o : n>");
            phono.ValidateSyllableRule("asihon", "<a>  <s :: i>  <h :: o : n>");
            phono.ValidateSyllableRule("basih", "<b :: a>  <s :: i> h");
            phono.ValidateSyllableRule("bai", "<b :: a : i>");
            phono.End();
        }

        [Test]
        public void SyllableCVCOnsetRequired()
        {
            var phono = new PhonixWrapper().StdImports().Append("syllable (onsetRequired) onset [+cons] nucleus [-cons +son] coda [+son]");
            phono.Start("-d");
            phono.ValidateSyllableRule("basihon", "<b :: a>  <s :: i>  <h :: o : n>");
            phono.ValidateSyllableRule("asihon", "a <s :: i>  <h :: o : n>");
            phono.ValidateSyllableRule("basih", "<b :: a>  <s :: i> h");
            phono.ValidateSyllableRule("bai", "<b :: a : i>");
            phono.End();
        }

        [Test]
        public void SyllableCVCCodaRequired()
        {
            var phono = new PhonixWrapper().StdImports().Append("syllable (codaRequired) onset [+cons] nucleus [-cons +son] coda [+cons]");
            phono.Start("-d");
            phono.ValidateSyllableRule("basihon", "<b :: a : s>  <i : h>  <o : n>");
            phono.ValidateSyllableRule("asihon", "<a : s>  <i : h>  <o : n>");
            phono.ValidateSyllableRule("basih", "<b :: a : s>  <i : h>");
            phono.ValidateInOut("bai", "bai"); // doesn't syllabify, so validate in-out
            phono.End();
        }

        [Test]
        public void SyllableCVCOnsetAndCodaRequired()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable (onsetRequired codaRequired) " + 
                    "onset [+cons] nucleus [-cons +son] coda [+cons]");
            phono.Start("-d");
            phono.ValidateSyllableRule("basihon", "<b :: a : s> i <h :: o : n>");
            phono.ValidateSyllableRule("asihon", "a <s :: i : h> on");
            phono.ValidateSyllableRule("basih", "<b :: a : s> ih");
            phono.ValidateInOut("bai", "bai"); // doesn't syllabify, so validate in-out
            phono.End();
        }

        [Test]
        public void SyllableCCV()
        {
            var phono = new PhonixWrapper().StdImports().Append("syllable onset [+cons]([+cons]) nucleus [-cons +son]");
            phono.Start("-d");
            phono.ValidateSyllableRule("basiho", "<b :: a>  <s :: i>  <h :: o>");
            phono.ValidateSyllableRule("brastihno", "<br :: a>  <st :: i>  <hn :: o>");
            phono.ValidateSyllableRule("aszihxon", "<a>  <sz :: i>  <hx :: o> n");
            phono.ValidateSyllableRule("barsih", "<b :: a>  <rs :: i> h");
            phono.End();
        }

        [Test]
        public void SyllableCCVC()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable onset ([-son])([+cons]) nucleus [-cons +son] coda [+son]");
            phono.Start("-d");
            phono.ValidateSyllableRule("basihon", "<b :: a>  <s :: i>  <h :: o : n>");
            phono.ValidateSyllableRule("bramstihnorl", "<br :: a : m>  <st :: i>  <hn :: o : r> l");
            phono.ValidateSyllableRule("alszihxon", "<a : l>  <sz :: i>  <hx :: o : n>");
            phono.ValidateSyllableRule("barsih", "<b :: a : r>  <s :: i> h");
            phono.ValidateSyllableRule("btai", "<bt :: a : i>");
            phono.End();
        }

        [Test]
        public void SyllableCCVCC()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable onset ([-son])([+cons]) nucleus [-cons +son] coda ([+son])([+cons])");
            phono.Start("-d");
            phono.ValidateSyllableRule("basihon", "<b :: a>  <s :: i>  <h :: o : n>");
            phono.ValidateSyllableRule("bramstihnorl", "<br :: a : m>  <st :: i>  <hn :: o : rl>");
            phono.ValidateSyllableRule("alszihxon", "<a : l>  <sz :: i>  <hx :: o : n>");
            phono.ValidateSyllableRule("barsih", "<b :: a : r>  <s :: i : h>");
            phono.ValidateSyllableRule("btaif", "<bt :: a : if>");
            phono.End();
        }

        [Test]
        public void SyllableCVCNucleusRight()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable (nucleusPreference=right) " + 
                    "onset [+cons]([+son]) nucleus [-cons +son] coda []");
            phono.Start("-d");
            phono.ValidateSyllableRule("bui", "<bu :: i>");
            phono.ValidateSyllableRule("biu", "<bi :: u>");
            phono.End();
        }

        [Test]
        public void SyllableCVCNucleusLeft()
        {
            var phono = new PhonixWrapper().StdImports().Append(
                    "syllable (nucleusPreference=left) " + 
                    "onset [+cons][+son] onset [+cons] nucleus [-cons +son] coda []");
            phono.Start("-d");
            phono.ValidateSyllableRule("bui", "<b :: u : i>");
            phono.ValidateSyllableRule("biu", "<b :: i : u>");
            phono.End();
        }
    }
}
