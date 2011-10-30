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

        public event Action<string> ParseBegin;
        public event Action<string> ParseEnd;

        public void Parse(Phonology phono)
        {
            if (ParseBegin == null)
            {
                ParseBegin += (s) => {};
            }
            if (ParseEnd == null)
            {
                ParseEnd += (s) => {};
            }

            try
            {
                ParseBegin(_currentFile);

                _phono = phono;
                parseRoot();

                if (_parseError)
                {
                    throw new ParseException(_currentFile);
                }
            }
            finally
            {
                ParseEnd(_currentFile);
            }
        }

        private void ParseImport(string importFile)
        {
            var importParser = FileParser(_currentFile, importFile);
            importParser.ParseBegin += (s) => this.ParseBegin(s);
            importParser.ParseEnd += (s) => this.ParseEnd(s);
            importParser.Parse(_phono);
        }

        // this is the public method for parsing a root file
        public static PhonixParser FileParser(string file)
        {
            return FileParser(null, file);
        }

        // this overload is used internally for parsing imports
        private static PhonixParser FileParser(string currentFile, string importedFile)
        {
            if (importedFile == null)
            {
                throw new ArgumentNullException("importedFile");
            }

            try
            {
                // first try opening the file directly
                var file = File.OpenText(importedFile);
                return GetParserForStream(importedFile, file);
            }
            catch (FileNotFoundException)
            {
                // look for a file in the same directory as the current file
                try
                {
                    if (currentFile == null)
                    {
                        throw new FileNotFoundException(null, importedFile);
                    }

                    // only attempt this block if the currentFile is not null
                    var currentFilePath = Path.GetFullPath(currentFile);
                    var currentFileDir = Path.GetDirectoryName(currentFilePath);
                    var importPath = Path.Combine(currentFileDir, importedFile);

                    var file = File.OpenText(importPath);
                    return GetParserForStream(importPath, file);
                }
                catch (FileNotFoundException)
                {
                    // look for an embedded resource. Exceptions thrown here are allowed to propagate.
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(importedFile);
                    if (stream == null)
                    {
                        throw new FileNotFoundException(null, importedFile);
                    }
                    return GetParserForStream(importedFile, new StreamReader(stream));
                }
            }
        }

        public static PhonixParser StringParser(string str)
        {
            StringReader reader = new StringReader(str);
            return GetParserForStream("<string>", reader);
        }

        private static PhonixParser GetParserForStream(string filename, TextReader stream)
        {
            var lexStream = new ANTLRReaderStream(stream);
            var lexer = new PhonixLexer(lexStream);
            var tokenStream = new CommonTokenStream();
            tokenStream.TokenSource = lexer;

#if debug
            var tracer = new PhonixDebugTracer(tokenStream);
            var parser = new PhonixParser(tokenStream, tracer);
#else
            var parser = new PhonixParser(tokenStream);
#endif

            parser._currentFile = filename;
            return parser;
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
