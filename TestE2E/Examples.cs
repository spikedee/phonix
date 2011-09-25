using System;
using System.Diagnostics;
using System.IO;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class ExamplesTest
    {
        [Test]
        public void Romanian()
        {
            var proc = Process.Start(
                    "phonix", 
                    "../examples/romanian.phonix -i ../examples/romanian.input -o ../examples/romanian.test.output");
            proc.WaitForExit();
            ParserTest.CompareFiles("../examples/romanian.output", "../examples/romanian.test.output");
        }
    }
}
