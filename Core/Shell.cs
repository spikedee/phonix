using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Phonix.Parse;

namespace Phonix
{
    public static class Shell
    {
        public const string FileRevision = "$Revision$";
        public const string FileURL = "$HeadURL$";

        private static StringBuilder _inputBuffer = new StringBuilder();

#if test
        // this is used to force exceptions inside Main()
        public static event Action TestCallback;
#endif

        internal enum ExitCode : int
        {
            Success = 0,
            BadArgument = 1,
            ParseError = 2,
            FileNotFound = 3,
            FatalWarning = 4,
            UnhandledException = Int32.MaxValue
        }

        internal class Config
        {
            public string PhonixFile;
            public TextReader Reader = Console.In;
            public TextWriter Writer = Console.Out;
            public Log.Level LogLevel = Log.Level.Warning;
            public Log.Level WarningLevel = Log.Level.Error;
        }

        public static int Main(params string[] args)
        {
            ExitCode rv = ExitCode.Success;

            // set up our crash handler
            AppDomain.CurrentDomain.UnhandledException += 
                (src, exArgs) => CrashHandler.GetReport(exArgs, new StringReader(_inputBuffer.ToString()));

            Config config = null;
            Log logger = null;
            try
            {
#if test
                // call the test callback if necessary
                var callback = TestCallback;
                if (callback != null)
                {
                    callback();
                }
#endif
                config = ParseArgs(args);

                Phonology phono = new Phonology();
                PhonixParser parser = PhonixParser.FileParser(config.PhonixFile);
                logger = new Log(config.LogLevel, config.WarningLevel, Console.Error, phono, parser);
                logger.Start();

                parser.Parse(phono);
                InputLoop(phono, config.Reader, config.Writer, Console.Error);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                rv = ExitCode.BadArgument;
            }
            catch (ParseException px)
            {
                Console.Error.WriteLine(px.Message);
                rv = ExitCode.ParseError;
            }
            catch (FileNotFoundException fex)
            {
                Console.Error.WriteLine("Could not find file '{0}'.", fex.FileName);
                rv = ExitCode.FileNotFound;
            }
            catch (FatalWarningException)
            {
                Console.Error.WriteLine("Exiting due to errors");
                rv = ExitCode.FatalWarning;
            }
            finally
            {
                if (logger != null)
                {
                    logger.Stop();
                }
                if (config != null)
                {
                    config.Reader.Close();
                    config.Writer.Close();
                }
            }

            return (int) rv;
        }

        public static void InputLoop(Phonology phono, TextReader reader, TextWriter writer, TextWriter err)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                _inputBuffer.AppendLine(line);

                Word word;
                try
                {
                    word = new Word(phono.SymbolSet.Pronounce(line));
                }
                catch (SpellingException ex)
                {
                    err.WriteLine(ex.Message);
                    continue;
                }
                phono.RuleSet.ApplyAll(word);

                writer.WriteLine(SafeMakeString(phono, word, err));
            }
        }

        public static string SafeMakeString(Phonology phono, Word word, TextWriter err)
        {
            StringBuilder str = new StringBuilder();
            foreach(var seg in word)
            {
                try
                {
                    str.Append(phono.SymbolSet.Spell(seg.Matrix).Label);
                }
                catch (SpellingException ex)
                {
                    err.WriteLine(ex.Message);
                    str.Append(seg.Matrix.ToString());
                }
            }
            return str.ToString();
        }

        internal static Config ParseArgs(string[] args)
        {
            var rv = new Config();

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];

                    switch (arg)
                    {
                        case "-i":
                        case "--in":
                        {
                            if (++i >= args.Length)
                            {
                                throw new ArgumentException("Argument required for " + arg);
                            }
                            string file = args[i];
                            rv.Reader = File.OpenText(file);
                            break;
                        }

                        case "-o":
                        case "--out":
                        {
                            if (++i >= args.Length)
                            {
                                throw new ArgumentException("Argument required for " + arg);
                            }
                            string file = args[i];
                            rv.Writer = File.CreateText(file);
                            break;
                        }

                        case "-d":
                        case "--debug":
                            rv.LogLevel = Log.Level.Debug;
                            break;

                        case "-q":
                        case "--quiet":
                            rv.LogLevel = Log.Level.Error;
                            break;

                        case "-w":
                        case "--warn-fatal":
                            rv.WarningLevel = Log.Level.Warning;
                            break;

                        case "-v":
                        case "--verbose":
                            rv.LogLevel = Log.Level.Verbose;
                            break;

                        case "-h":
                        case "--help":
                            /* Using an exception here is effective, but kind of gross */
                            throw new ArgumentException(HelpMessage);

                        case "--version":
                            /* Using an exception here is effective, but kind of gross */
                            throw new ArgumentException(VersionMessage);

                        default:
                            rv.PhonixFile = arg;
                            break;
                    }
                }
            }
            catch (FileNotFoundException fex)
            {
                throw new ArgumentException(fex.Message, fex);
            }

            if (rv.PhonixFile == null)
            {
                throw new ArgumentException("You must give the name of a Phonix file");
            }

            return rv;
        }

        private static string HelpMessage
        {
            get
            {
                return
                    "phonix [language-file] -i [input-file] -o [output-file]\n" +
                    "   -i, --input         specify the lexicon input file\n" +
                    "   -o, --output        specify the lexicon output file\n" +
                    "   -q, --quiet         print only fatal errors\n" +
                    "   -d, --debug         print debugging messages\n" +
                    "   -v, --verbose       print extra verbose imessages (more than -d)\n" +
                    "   -w, --warn-fatal    treat all warnings as fatal errors\n" +
                    "   -h, --help          show this help message\n" +
                    "   --version           show the phonix version\n\n" +
                    "For additional help, consult the Phonix manual available online at http://phonix.googlecode.com";
            }
        }

        private static string VersionMessage
        {
            get
            {
                return String.Format("phonix {0}, (c) 2011 by Jesse Bangs", Version);
            }
        }

        public static string Version
        {
            get
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("version");
                var version = new StreamReader(stream).ReadToEnd();
                return version.Substring(0, version.Length - 1);
            }
        }
    }
}
