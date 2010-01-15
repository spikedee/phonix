using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Phonix
{
    public static class CrashHandler
    {
        public static Uri BugReportUrl = new Uri("http://code.google.com/p/phonix/wiki/CrashReport");
        public static string CrashReportFile = "PhonixCrashReport.txt";

        public static void GetReport(UnhandledExceptionEventArgs args, StringReader inputReader)
        {
            Console.WriteLine();
            Console.WriteLine("!! CRASH !!");
            Console.WriteLine("Phonix has encountered an unexpected error and needs to close. This is");
            Console.WriteLine("probably because of a bug in Phonix. Would you like to file a bug");
            Console.WriteLine("report in our online bug tracking system?");
            Console.WriteLine();
            Console.Write("[y/N] ");

            var keyInfo = Console.ReadKey();
            Console.WriteLine();

            if (Char.ToLowerInvariant(keyInfo.KeyChar) != 'y')
            {
                // they don't want to report a bug. boo-hoo.
                Console.WriteLine(args.ExceptionObject.ToString());
            }
            else
            {
                // launch the browser on the bug page
                Process.Start(BugReportUrl.ToString());

                // create the crash report file
                BuildReportFile(args.ExceptionObject as Exception, inputReader);
                Console.WriteLine("Phonix has created a file called {0} in your current directory. Please attach this file to your bug report.", CrashReportFile);
            }
        }

        internal static void BuildReportFile(Exception ex, StringReader inputReader)
        {
            using (var report = new StreamWriter(File.OpenWrite(CrashReportFile)))
            {
                var args = Environment.GetCommandLineArgs();
                report.WriteLine("Revision: {0}", Shell.FileRevision);
                report.WriteLine("URL: {0}", Shell.FileURL);
                report.WriteLine("Cmdline Args: {0}", String.Join(" ", args));
                report.WriteLine("Exception: {0}", ex.ToString());

                report.WriteLine();
                report.WriteLine("===========");
                report.WriteLine("Phonix File");
                report.WriteLine("===========");
                report.WriteLine();

                var config = Shell.ParseArgs(args);
                if (File.Exists(config.PhonixFile))
                {
                    using (var phonixFile = File.OpenText(config.PhonixFile))
                    {
                        while (!phonixFile.EndOfStream)
                        {
                            report.WriteLine(phonixFile.ReadLine());
                        }
                    }
                }
                else
                {
                    report.WriteLine("[No such file]");
                }

                report.WriteLine();
                report.WriteLine("===========");
                report.WriteLine("Input Data");
                report.WriteLine("===========");
                report.WriteLine();

                string line;
                while ((line = inputReader.ReadLine()) != null)
                {
                    report.WriteLine(line);
                }
            }
        }
    }
}
