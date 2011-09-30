using System;
using System.Diagnostics;
using System.IO;
using System.Text;

// this class contains a wrapper for invocation of the phonix executable. it
// supports a fluent interface for building up the phonix file and executing
// it, as well as a variety of methods for validating its input and output.
// this is only really expected to work on linux right now.

namespace Phonix.TestE2E
{
    using NUnit.Framework;

    internal class PhonixWrapper : IDisposable
    {
        private readonly string filename;
        private readonly StringBuilder fileContents;
        private Process phonixProcess;

        internal PhonixWrapper()
        {
            this.fileContents = new StringBuilder();
        }

        internal PhonixWrapper(string phonixFilename)
        {
            this.filename = phonixFilename;
        }

        internal PhonixWrapper StdImports()
        {
            fileContents.AppendLine("import std.features");
            fileContents.AppendLine("import std.symbols");
            return this;
        }

        internal PhonixWrapper Append(string line)
        {
            fileContents.AppendLine(line);
            return this;
        }

        internal PhonixWrapper Start()
        {
            if (phonixProcess != null)
            {
                throw new InvalidOperationException("Must first End() previous instance in PhonixWrapper");
            }
            string phonixFile = Path.GetTempFileName();
            File.WriteAllText(phonixFile, fileContents.ToString());

            var psi = new ProcessStartInfo();
            psi.FileName = "phonix";
            psi.Arguments = phonixFile;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            phonixProcess = Process.Start(psi);
            return this;
        }

        internal PhonixWrapper Start(string inputFilename, string outputFilename)
        {
            if (phonixProcess != null)
            {
                throw new InvalidOperationException("Must first End() previous instance in PhonixWrapper");
            }
            phonixProcess = Process.Start("phonix", String.Format("{0} -i {1} -o {2}", this.filename, inputFilename, outputFilename));
            return this;
        }

        internal PhonixWrapper End()
        {
            if (phonixProcess == null)
            {
                throw new InvalidOperationException("No instance of phonix is started");
            }
            if (fileContents != null)
            {
                phonixProcess.StandardInput.Close();
            }
            phonixProcess.WaitForExit();
            phonixProcess.Dispose();
            phonixProcess = null;

            return this;
        }

        internal PhonixWrapper ValidateInOut(string input, string expectedOut)
        {
            if (phonixProcess == null)
            {
                throw new InvalidOperationException("No instance of phonix is started");
            }
            phonixProcess.StandardInput.WriteLine(input);
            phonixProcess.StandardInput.Flush();

            string actualOut = phonixProcess.StandardOutput.ReadLine();
            Assert.AreEqual(expectedOut, actualOut);

            return this;
        }

        internal void CompareFiles(string expectedFile, string testFile)
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

        internal void ApplySyllableRule(string input, string syllableOutput)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (phonixProcess != null)
            {
                this.End();
            }
        }
    }
}
