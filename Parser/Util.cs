using Antlr.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    public class RuleContext
    {
        public List<IRuleSegment> Left;
        public List<IRuleSegment> Right;

        public RuleContext()
        {
            Left = new List<IRuleSegment>();
            Right = new List<IRuleSegment>();
        }
    }

    public static class Util
    {
        public static Feature MakeFeature(string name, ParamList plist, Phonology phono)
        {
            Feature f = null;
            bool isNode = false;
            IEnumerable<Feature> childList = null;

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

                                case "node":
                                    isNode = true;
                                    break;

                                default:
                                    throw new InvalidParameterValueException(key, type);
                            }
                        }
                        break;

                        case "children":
                        {
                            var list = new List<Feature>();
                            foreach (string child in (val as string).Split(','))
                            {
                                list.Add(phono.FeatureSet.Get<Feature>(child.Trim()));
                            }
                            childList = list;
                        }
                        break;

                        default:
                            throw new UnknownParameterException(key);
                    }
                }
            }

            if (isNode)
            {
                if (childList == null)
                {
                    throw new InvalidParameterValueException("node", "no 'children' parameter found");
                }
                f = new NodeFeature(name, childList);
            }
            else if (childList != null)
            {
                throw new InvalidParameterValueException("children", "not allowed except on feature nodes");
            }

            Debug.Assert(f != null, "f != null");

            return f;
        }

        public static Rule MakeRule(string name, IEnumerable<IRuleSegment> action, RuleContext context, RuleContext excluded, ParamList plist)
        {
            var contextSegs = new List<IRuleSegment>();
            var excludedSegs = new List<IRuleSegment>();

            if (context == null)
            {
               context = new RuleContext();
            }
            if (excluded == null)
            {
                // always add a non-matching segment to the excluded context if it's null
                excluded = new RuleContext();
                excluded.Right.Add(new FeatureMatrixSegment(MatrixMatcher.NeverMatches, MatrixCombiner.NullCombiner));
            }

            // add the context segments into their list
            contextSegs.AddRange(context.Left);
            contextSegs.AddRange(action);
            contextSegs.AddRange(context.Right);

            // the left sides of the context and the exclusion need to
            // be aligned. We add StepSegments (which only advance the
            // cursor) or BackstepSegments (which only move back the
            // cursor) to accomplish this
            int diff = context.Left.Count - excluded.Left.Count;
            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    excludedSegs.Add(new StepSegment());
                }
            }
            else if (diff < 0)
            {
                for (int i = 0; i > diff; i--)
                {
                    excludedSegs.Add(new BackstepSegment());
                }
            }

            excludedSegs.AddRange(excluded.Left);
            for (int i = 0; i < action.Count(); i++)
            {
                excludedSegs.Add(new StepSegment());
            }
            excludedSegs.AddRange(excluded.Right);

            var rule = new Rule(name, contextSegs, excludedSegs);
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
                            switch (dir.ToLowerInvariant())
                            {
                                case "right-to-left":
                                    rule.Direction = Direction.Leftward;
                                    break;

                                case "left-to-right":
                                    rule.Direction = Direction.Rightward;
                                    break;

                                default:
                                    throw new InvalidParameterValueException(key, dir);
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

            parser.parseRoot(currentFile, phono);
        }

        public static void ParseString(Phonology phono, string str)
        {
            StringReader reader = new StringReader(str);
            PhonixParser parser = GetParserForStream(reader);

            parser.parseRoot("$string", phono);
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

        internal class PhonixDebugTracer : Antlr.Runtime.Debug.Tracer
        {
            public PhonixDebugTracer(ITokenStream stream)
                : base(stream)
            {
            }
        }
    }

}

