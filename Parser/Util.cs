using Antlr.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System;

namespace Phonix.Parse
{
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

            bool isScalar = false;
            int? scalarMin = null;
            int? scalarMax = null;

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
                                    isScalar = true;
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

                        case "min":
                        {
                            int intVal;
                            if (!Int32.TryParse(val as string, out intVal))
                            {
                                throw new InvalidParameterValueException(key, "not an integer");
                            }
                            scalarMin = intVal;
                        }
                        break;

                        case "max":
                        {
                            int intVal;
                            if (!Int32.TryParse(val as string, out intVal))
                            {
                                throw new InvalidParameterValueException(key, "not an integer");
                            }
                            scalarMax = intVal;
                        }
                        break;

                        default:
                            throw new UnknownParameterException(key);
                    }
                }
            }

            // resolve node features based on parameters
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

            // resolve scalar features based on parameters
            if (scalarMin.HasValue || scalarMax.HasValue)
            {
                if (!isScalar)
                {
                    throw new InvalidParameterValueException("min/max", "not allowed except on scalar features");
                }
                else if (!(scalarMin.HasValue && scalarMax.HasValue))
                {
                    throw new InvalidParameterValueException("min/max", "both min and max required");
                }
                else
                {
                    f = new ScalarFeature(name, scalarMin.Value, scalarMax.Value);
                }
            }
            else if (isScalar)
            {
                f = new ScalarFeature(name);
            }

            Debug.Assert(f != null, "f != null");

            return f;
        }

        public static void MakeAndAddSymbol(string label, FeatureMatrix matrix, ParamList plist, SymbolSet symbolSet)
        {
            if (plist != null && plist.ContainsKey("diacritic"))
            {
                if (plist["diacritic"] != null)
                {
                    throw new InvalidParameterValueException("diacritic", plist["diacritic"].ToString());
                }
                symbolSet.AddDiacritic(new Diacritic(label, matrix));
            }
            else
            {
                symbolSet.Add(new Symbol(label, matrix));
            }
        }

        public static void MakeAndAddRule(string name, IEnumerable<IRuleSegment> action, RuleContext context, RuleContext excluded, ParamList plist, RuleSet ruleSet)
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
                excluded.Right.Add(new ContextSegment(MatrixMatcher.NeverMatches));
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

                        case "applicationRate":
                        {
                            double rate;
                            string strRate = val as string;
                            if (!double.TryParse(strRate, out rate))
                            {
                                throw new InvalidParameterValueException(key, strRate + " (value was not a decimal)");
                            }
                            try
                            {
                                rule.ApplicationRate = rate;
                            }
                            catch (ArgumentException)
                            {
                                throw new InvalidParameterValueException(key, strRate + " (value was not between 0 and 1)");
                            }
                        }
                        break;

                        case "persist":
                            // no-op, persistent rules are handled below
                            break;

                        default:
                            throw new UnknownParameterException(key);
                    }
                }
            }

            if (plist != null && plist.ContainsKey("persist"))
            {
                if (plist["persist"] != null)
                {
                    throw new InvalidParameterValueException("persist", plist["persist"].ToString());
                }
                ruleSet.AddPersistent(rule);
            }
            else
            {
                ruleSet.Add(rule);
            }
        }

        public static void MakeAndAddSyllable(SyllableBuilder syll, ParamList plist, RuleSet ruleSet)
        {
            bool onsetRequired = false;
            bool codaRequired = false;
            bool persist = false;

            if (plist != null)
            {
                foreach (var key in plist.Keys)
                {
                    switch (key)
                    {
                        case "onsetRequired":
                            onsetRequired = true;
                        break;

                        case "codaRequired":
                            codaRequired = true;
                        break;

                        case "nucleusPreference":
                        {
                            var pref = plist["nucleusPreference"];
                            if (pref == null)
                            {
                                throw new InvalidParameterValueException("nucleusPreference", "<empty");
                            }
                            else if (pref.Equals("left"))
                            {
                                syll.Direction = SyllableBuilder.NucleusDirection.Left;
                            }
                            else if (pref.Equals("right"))
                            {
                                syll.Direction = SyllableBuilder.NucleusDirection.Right;
                            }
                            else
                            {
                                throw new InvalidParameterValueException("nucleusPreference", pref.ToString());
                            }
                            break;
                        }

                        case "persist":
                            persist = true;
                        break;

                        default:
                            throw new UnknownParameterException(key);
                    }
                }
            }

            if (!onsetRequired)
            {
                // onsets not required, so add the implicit empty onset
                syll.Onsets.Add(Enumerable.Empty<IMatrixMatcher>());
            }
            if (!codaRequired)
            {
                // codas not required, so add the implicit empty coda
                syll.Codas.Add(Enumerable.Empty<IMatrixMatcher>());
            }

            var rule = syll.GetSyllableRule();
            if (persist)
            {
                ruleSet.AddPersistent(rule);
            }
            else
            {
                ruleSet.Add(rule);
            }
        }

        public static List<IRuleSegment> MakeRuleAction(
                List<IMatrixMatcher> left, 
                List<IMatrixCombiner> right)
        {
            if (left.Count != right.Count)
            {
                var msg = String.Format(
                        "unbalanced rule action ({0} segments before '=>', {1} segments after)", 
                        left.Count, 
                        right.Count);
                throw new RuleFormatException(msg);
            }

            var result = new List<IRuleSegment>();
            var leftObj = left.GetEnumerator();
            var rightObj = right.GetEnumerator();

            while (leftObj.MoveNext() && rightObj.MoveNext())
            {
                if (leftObj.Current == null && rightObj.Current == null)
                {
                    throw new RuleFormatException("can't map zero to zero");
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
                    result.Add(new ActionSegment(leftObj.Current, rightObj.Current));
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
                    if (stream == null)
                    {
                        throw new FileNotFoundException(filename);
                    }
                    parser = GetParserForStream(new StreamReader(stream));
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

