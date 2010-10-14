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
    }
}
