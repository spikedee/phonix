using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Phonix
{
    public static class Shell
    {
        public const string FileRevision = "$Revision$";
        public const string FileURL = "$HeadURL$";

        public static int Main(string[] args)
        {
            int rv = 0;

            PhonixConfig config = null;
            try
            {
                config = ParseArgs(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(2);
            }

            Phonology phono = new Phonology();
            var logger = new Logger(config.LogLevel, config.WarningLevel, Console.Error, phono);
            logger.Start();

            try
            {
                Parse.Util.ParseFile(phono, config.PhonixFile, config.PhonixFile);
                InputLoop(phono, config.Reader, config.Writer, Console.Error);
            }
            catch (ParseException px)
            {
                Console.Error.WriteLine(px.Message);
                rv = 1;
            }
            catch (FileNotFoundException fex)
            {
                Console.Error.WriteLine("Could not find file '{0}'.", fex.Message);
                rv = 2;
            }
            catch (Exception ex)
            {
                // this should catch all unexpected errors
                Console.Error.WriteLine(ex.ToString());
                rv = Int32.MaxValue;
            }

            logger.Stop();

            config.Reader.Close();
            config.Writer.Close();

            return rv;
        }

        public static void InputLoop(Phonology phono, TextReader reader, TextWriter writer, TextWriter err)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
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
            foreach(var fm in word)
            {
                try
                {
                    str.Append(phono.SymbolSet.Spell(fm).Label);
                }
                catch (SpellingException ex)
                {
                    err.WriteLine(ex.Message);
                    str.Append(fm.ToString());
                }
            }
            return str.ToString();
        }

        public static PhonixConfig ParseArgs(string[] args)
        {
            var rv = new PhonixConfig();

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
                            rv.LogLevel = Level.Info;
                            break;

                        case "-q":
                        case "--quiet":
                            rv.LogLevel = Level.Error;
                            break;

                        case "-w":
                        case "--warn-fatal":
                            rv.WarningLevel = Level.Warning;
                            break;

                        case "-v":
                        case "--verbose":
                            rv.LogLevel = Level.Verbose;
                            break;

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

    }

    public class PhonixConfig
    {
        public string PhonixFile;
        public TextReader Reader = Console.In;
        public TextWriter Writer = Console.Out;
        public Level LogLevel = Level.Warning;
        public Level WarningLevel = Level.Error;
    }

}
