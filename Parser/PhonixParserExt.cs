using Antlr.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Phonix.Parse
{
#if debug
    using Antlr.Runtime.Debug;
    public partial class PhonixParser : DebugParser
#else
    public partial class PhonixParser : Parser
#endif
    {
        private Phonology _phono;
        private string _currentFile;
        private bool _parseError;
        private readonly Dictionary<string, IEnumerable<FeatureValue>> _featureValueGroups = 
            new Dictionary<string, IEnumerable<FeatureValue>>();

        private void Parse(string currentFile, Phonology phono)
        {
            _currentFile = currentFile;
            _phono = phono;

            parseRoot();
            if (_parseError)
            {
                throw new ParseException(currentFile);
            }
        }

        public static void ParseFile(Phonology phono, string currentFile, string filename)
        {
            if (currentFile == null || filename == null)
            {
                throw new ArgumentNullException();
            }

            PhonixParser parser = null;

            try
            {
                // first try opening the file directly
                var file = File.OpenText(filename);
                parser = GetParserForStream(file);
            }
            catch (FileNotFoundException)
            {
                // look for a file in the same directory as the current file
                try
                {
                    var fullCurrentPath = Path.GetFullPath(currentFile);
                    var currentDir = Path.GetDirectoryName(fullCurrentPath);
                    var file = File.OpenText(Path.Combine(currentDir, filename));
                    parser = GetParserForStream(file);
                }
                catch (FileNotFoundException)
                {
                    // look for an embedded resource. Exceptions thrown here are allowed to propagate.
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename);
                    if (stream == null)
                    {
                        throw new FileNotFoundException(filename);
                    }
                    parser = GetParserForStream(new StreamReader(stream));
                }
            }

            parser.Parse(currentFile, phono);
        }

        public static void ParseString(Phonology phono, string str)
        {
            StringReader reader = new StringReader(str);
            PhonixParser parser = GetParserForStream(reader);

            parser.Parse("$string", phono);
        }

        private static PhonixParser GetParserForStream(TextReader stream)
        {
            var lexStream = new ANTLRReaderStream(stream);
            var lexer = new PhonixLexer(lexStream);
            var tokenStream = new CommonTokenStream();
            tokenStream.TokenSource = lexer;

#if debug
            var tracer = new PhonixDebugTracer(tokenStream);
            return new PhonixParser(tokenStream, tracer);
#else
            return new PhonixParser(tokenStream);
#endif
        }

        private FeatureMatrix GetSingleSymbol(List<Symbol> symbols)
        {
            if (symbols.Count != 1)
            {
                string symbolStr = String.Join("", symbols.ConvertAll(s => s.ToString()).ToArray());
                throw new InvalidMultipleSymbolException(symbolStr);
            }
            return symbols[0].FeatureMatrix;
        }

        private class PhonixDebugTracer : Antlr.Runtime.Debug.Tracer
        {
            public PhonixDebugTracer(ITokenStream stream)
                : base(stream)
            {
            }
        }
    }
}
