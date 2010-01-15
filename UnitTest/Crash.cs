using System;
using System.IO;
using System.Text;
using Phonix;

namespace Phonix.UnitTest
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
            inputData.AppendLine("test");
            inputData.AppendLine("data");

            CrashHandler.BuildReportFile(ex, new StringReader(inputData.ToString()));

            Assert.IsTrue(File.Exists(CrashHandler.CrashReportFile));

            var fileInfo = new FileInfo(CrashHandler.CrashReportFile);
            Assert.IsTrue(fileInfo.Length > 0);
        }
    }
}
