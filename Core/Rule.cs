using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{
    public enum Direction
    {
        Rightward,
        Leftward
    }

    public class Rule
    {
        public readonly string Name;

        public readonly IEnumerable<IRuleSegment> Segments;
        public readonly IEnumerable<IRuleSegment> ExcludedSegments;

        public IMatrixMatcher Filter { get; set; }
        public Direction Direction { get; set; }

        // Application rate should vary from 0 to 1000
        private int _applicationRate = 1000;
        public double ApplicationRate
        {
            get { return ((double)_applicationRate) / 1000; }
            set { 
                if (value < 0 || value > 1)
                {
                    throw new ArgumentException("ApplicationRate must be between zero and one");
                }
                _applicationRate = (int)(value * 1000);
            }
        }

        private string _description;
        public string Description
        {
            get
            {
                if (_description == null)
                {

                    // build the description string. this is kinda complicated,
                    // which is why we don't do it until we're asked (and we
                    // save the result)
                    
                    var leftCtx = new StringBuilder();
                    var rightCtx = new StringBuilder();
                    var leftAct = new StringBuilder();
                    var rightAct = new StringBuilder();
                    bool leftSide = true;

                    // iterate over all of the segments here, and append their
                    // strings to the left or right context or left/right action as
                    // necessary. We start by putting match segments on the left
                    // context, then switch to the right context as soon as we've
                    // encountered any action segments.
                    
                    foreach (var seg in Segments)
                    {
                        if (seg.IsMatchOnlySegment)
                        {
                            if (leftSide)
                            {
                                leftCtx.Append(seg.MatchString);
                            }
                            else
                            {
                                rightCtx.Append(seg.MatchString);
                            }
                        }
                        else
                        {
                            leftAct.Append(seg.MatchString);
                            rightAct.Append(seg.CombineString);
                            leftSide = false;
                        }
                    }

                    _description = String.Format("{0} => {1} / {2} _ {3}", 
                            leftAct.ToString(), 
                            rightAct.ToString(), 
                            leftCtx.ToString(), 
                            rightCtx.ToString());
                }
                return _description;
            }
        }

        private Random _random = new Random();

        public event Action<Rule, Word> Entered;
        public event Action<Rule, Word, IWordSlice> Applied;
        public event Action<Rule, Word> Exited;
        public event Action<Rule, IFeatureValue> UndefinedVariableUsed;
        public event Action<Rule, ScalarFeature, int> ScalarValueRangeViolation;
        public event Action<Rule, ScalarFeature, string> InvalidScalarValueOp;

        public Rule(string name, IEnumerable<IRuleSegment> segments, IEnumerable<IRuleSegment> excluded)
        {
            if (name == null || segments == null || excluded == null)
            {
                throw new ArgumentNullException();
            }

            Name = name;
            Segments = segments;
            ExcludedSegments = excluded;

            // add empty event handlers
            Entered += (r, w) => {};
            Applied += (r, w, s) => {};
            Exited += (r, w) => {};
            UndefinedVariableUsed += (r, v) => {};
            ScalarValueRangeViolation += (r, f, v) => {};
            InvalidScalarValueOp += (r, f, s) => {};
        }

        public override string ToString()
        {
            return Name;
        }

        public void Apply(Word word)
        {
            Entered(this, word);

            try
            {
                var slice = word.GetSliceEnumerator(Direction, Filter);

                while (slice.MoveNext())
                {
                    if (_applicationRate < 1000)
                    {
                        if (_random.Next(0, 1000) > _applicationRate)
                        {
                            // skip this potential application of the rule
                            continue;
                        }
                    }

                    // match all of the segments
                    var context = new RuleContext();
                    var matrix = slice.Current.GetEnumerator();
                    bool matchedAll = true;
                    foreach (var segment in Segments)
                    {
                        if (!segment.Matches(context, matrix))
                        {
                            matchedAll = false;
                            break;
                        }
                    }
                    matrix.Dispose();

                    if (!matchedAll)
                    {
                        continue;
                    }

                    // ensure that we don't match the excluded segments
                    matrix = slice.Current.GetEnumerator();
                    bool matchedExcluded = true;
                    foreach (var segment in ExcludedSegments)
                    {
                        if (!segment.Matches(context, matrix))
                        {
                            matchedExcluded = false;
                            break;
                        }
                    }
                    matrix.Dispose();

                    if (matchedExcluded)
                    {
                        continue;
                    }

                    // apply all of the segments
                    var wordSegment = slice.Current.GetMutableEnumerator();
                    foreach (var segment in Segments)
                    {
                        try
                        {
                            segment.Combine(context, wordSegment);
                        }
                        catch (UndefinedFeatureVariableException ex)
                        {
                            UndefinedVariableUsed(this, ex.Variable);
                        }
                        catch (ScalarValueRangeException ex)
                        {
                            ScalarValueRangeViolation(this, ex.Feature, ex.Value);
                        }
                        catch (InvalidScalarOpException ex)
                        {
                            InvalidScalarValueOp(this, ex.Feature, ex.Message);
                        }
                    }
                    wordSegment.Dispose();
                    Applied(this, word, slice.Current);
                }
            }
            finally
            {
                Exited(this, word);
            }
        }

    }
}
