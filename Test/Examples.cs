using System;
using System.IO;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class ExamplesTest
    {
        private void CompareFiles(string savedFile, string testFile)
        {
            var saved = File.OpenText(savedFile);
            var test = File.OpenText(testFile);

            while (!test.EndOfStream)
            {
                string testLine = test.ReadLine();
                string savedLine = saved.ReadLine();
                Assert.AreEqual(savedLine, testLine);
            }
            Assert.AreEqual(saved.EndOfStream, test.EndOfStream);
        }

        [Test]
        public void Romanian()
        {
            Phonix.Shell.Main(new string[] {
                    "../examples/romanian.phonix",
                    "-i",
                    "../examples/romanian.input",
                    "-o",
                    "../examples/romanian.test.output"
                    });
            CompareFiles("../examples/romanian.output", "../examples/romanian.test.output");
        }
    }
}
