using System;
using System.Diagnostics;
using System.IO;
using Phonix;

namespace Phonix.TestE2E
{
    using NUnit.Framework;

    [TestFixture]
    public class ExamplesTest
    {
        [Test]
        public void Romanian()
        {
            var phonix = new PhonixWrapper("../examples/romanian.phonix");
            phonix.Start("../examples/romanian.input", "../examples/romanian.test.output");
            phonix.End();
            phonix.CompareFiles("../examples/romanian.output", "../examples/romanian.test.output");
        }
    }
}
