using Antlr.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System;

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
        public static Feature MakeFeature(string name, ParamList plist)
        {
            Feature f = null;

            if (plist == null)
            {
                f = new BinaryFeature(name);
            }
            else
            {
                // set the type to binary if it doesn't exist yet
                if (!plist.ContainsKey("type"))
                {
                    plist["type"] = "binary";
                }

                foreach (string key in plist.Keys)
                {
                    object val = plist[key];
                    switch (key)
                    {
                        case "type":
                        {
                            Debug.Assert(val is string);
                            string type = val as string;
                            switch (type)
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
                                    throw new UnknownFeatureTypeException(type);
                            }
                        }
                        break;

                        default:
                            throw new UnknownParameterException(key);
                    }
                }
            }

            return f;
        }
        
        public static Rule MakeRule(string name, IEnumerable<IRuleSegment> segs, ParamList plist)
        {
            var rule = new Rule(name, segs);
            if (plist != null)
            {
                foreach (string key in plist.Keys)
                {
                    object val = plist[key];
                    switch (key)
                    {
                        case "filter":
                            Debug.Assert(val is FeatureMatrix);
                            rule.Filter = new MatrixMatcher(val as FeatureMatrix);
                            break;

                        case "direction":
                        {
                            string dir = val as string;
                            Debug.Assert(dir != null);
                            if (dir.ToLowerInvariant().Equals("right-to-left"))
                            {
                                rule.Direction = Direction.Leftward;
                            }
                            else if (dir.ToLowerInvariant().Equals("left-to-right"))
                            {
                                rule.Direction = Direction.Rightward;
                            }
                        }
                        break;

                        default:
                            throw new UnknownParameterException(key);
                    }
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

