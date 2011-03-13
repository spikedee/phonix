using System;
using System.IO;
using System.Text;
using Phonix;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class CrashTest
    {
        [Test]
        public void BuildReportFile()
        {
            var ex = new ApplicationException("unit test");
            var inputData = new StringBuilder();

            inputData.AppendLine("Unit");
            inputData.AppendLine("Test");
            inputData.AppendLine("Data");

            CrashHandler.BuildReportFile(ex, new StringReader(inputData.ToString()));

            Assert.IsTrue(File.Exists(CrashHandler.CrashReportFile));

            var fileInfo = new FileInfo(CrashHandler.CrashReportFile);
            Assert.IsTrue(fileInfo.Length > 0);
        }

        [Test]
        public void MainBadArgument()
        {
            Action callback = () => { throw new ArgumentException(); };
            try
            {
                Shell.TestCallback += callback;
                int rv = Shell.Main();
                Assert.AreEqual((int)Shell.ExitCode.BadArgument, rv);
            }
            finally
            {
                Shell.TestCallback -= callback;
            }
        }

        [Test]
        public void MainParseError()
        {
            Action callback = () => { throw new ParseException("bad test parse"); };
            try
            {
                Shell.TestCallback += callback;
                int rv = Shell.Main();
                Assert.AreEqual((int)Shell.ExitCode.ParseError, rv);
            }
            finally
            {
                Shell.TestCallback -= callback;
            }
        }

        [Test]
        public void MainFileNotFound()
        {
            Action callback = () => { throw new FileNotFoundException("fake_filename"); };
            try
            {
                Shell.TestCallback += callback;
                int rv = Shell.Main();
                Assert.AreEqual((int)Shell.ExitCode.FileNotFound, rv);
            }
            finally
            {
                Shell.TestCallback -= callback;
            }
        }

        [Test]
        public void MainFatalWarning()
        {
            Action callback = () => { throw new FatalWarningException("bad test warning"); };
            try
            {
                Shell.TestCallback += callback;
                int rv = Shell.Main();
                Assert.AreEqual((int)Shell.ExitCode.FatalWarning, rv);
            }
            finally
            {
                Shell.TestCallback -= callback;
            }
        }
    }
}
