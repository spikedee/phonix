using System;
using Antlr.Runtime;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Phonix.Parse
{
    public struct Param
    {
        public string Name;
        public string Value;
    }

    public class ParamList : Dictionary<string, object>
    {
    }

    public static class Util
    {
        public static Feature MakeFeature(string name, Dictionary<string, object> param)
        {
            Feature f = null;

            if (param == null)
            {
                f = new BinaryFeature(name);
            }
            else
            {
                if (!param.ContainsKey("type"))
                {
                    param["type"] = "binary";
                }

                switch (param["type"] as string)
                {
                    case "unary":
                        f = new UnaryFeature(name);
                        break;

                    case "binary":
                        f = new BinaryFeature(name);
                        break;

                    case "scalar":
                        f = new ScalarFeature(name);
                        break;

                    default:
                        throw new FeatureTypeException(name, param["type"] as string);
                }
            }

            return f;
        }
        
        public static Rule MakeRule(string name, IEnumerable<IRuleSegment> segs, ParamList plist)
        {
            var rule = new Rule(name, segs);
            if (plist != null)
            {
                if (plist.ContainsKey("filter"))
                {
                    rule.Filter = new MatrixMatcher(plist["filter"] as FeatureMatrix);
                }
            }
            return rule;
        }

        public static List<IRuleSegment> MakeRuleAction(
                List<IMatrixMatcher> left, 
                List<IMatrixCombiner> right)
        {
            if (left.Count != right.Count)
            {
                var msg = String.Format(
                        "unbalanced rule action ({0} segments on before '=>', {1} segments after)", 
                        left, 
                        right);
                throw new RuleException(msg);
            }

            var result = new List<IRuleSegment>();
            var leftObj = left.GetEnumerator();
            var rightObj = right.GetEnumerator();

            while (leftObj.MoveNext() && rightObj.MoveNext())
            {
                if (leftObj.Current == null && rightObj.Current == null)
                {
                    throw new RuleException("can't map zero to zero");
                }
                else if (leftObj.Current == null)
                {
                    result.Add(new InsertingSegment(rightObj.Current));
                }
                else if (rightObj.Current == null)
                {
                    result.Add(new DeletingSegment(leftObj.Current));
                }
                else
                {
                    result.Add(new FeatureMatrixSegment(leftObj.Current, rightObj.Current));
                }
            }

            return result;
        }

        public static Phonology ParseFile(string currentFile, string filename)
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
                    parser = GetParserForStream(new StreamReader(stream));
                }
            }
            finally
            {
                if (parser == null)
                {
                    throw new FileNotFoundException(filename);
                }
            }

            Phonology phono = new Phonology();
            parser.parseRoot(currentFile, phono);

            return phono;
        }

        public static PhonixParser GetParserForStream(TextReader stream)
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

        internal class PhonixDebugTracer : Antlr.Runtime.Debug.Tracer
        {
            public PhonixDebugTracer(ITokenStream stream)
                : base(stream)
            {
            }
        }
    }

}

