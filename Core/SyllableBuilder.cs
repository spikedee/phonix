using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Phonix
{
    public class SyllableBuilder
    {
        public enum NucleusDirection
        {
            Neutral,
            Left,
            Right
        }

        public readonly List<IEnumerable<IMatrixMatcher>> Onsets = new List<IEnumerable<IMatrixMatcher>>();
        public readonly List<IEnumerable<IMatrixMatcher>> Nuclei = new List<IEnumerable<IMatrixMatcher>>();
        public readonly List<IEnumerable<IMatrixMatcher>> Codas = new List<IEnumerable<IMatrixMatcher>>();
        public NucleusDirection Direction = NucleusDirection.Neutral;

        public AbstractRule GetSyllableRule()
        {
            if (Nuclei.Count == 0)
            {
                throw new InvalidOperationException("nuclei list cannot be empty");
            }

            var rs = new RuleSet();
            var list = new List<Syllable>();
            var rules = BuildRuleList(list);

            foreach (var rule in rules)
            {
                rs.Add(rule);
            }

            return new SyllableRule(rs, BuildDescription(), list, Direction);
        }

        private class SyllableRule : AbstractRule
        {
            private readonly RuleSet _rs;
            private readonly string _description;
            private readonly List<Syllable> _list;
            private readonly NucleusDirection _direction;

            public SyllableRule(RuleSet rs, string description, List<Syllable> syllableList, NucleusDirection direction)
                : base ("syllabify")
            {
                _rs = rs;
                _description = description;
                _list = syllableList;
                _direction = direction;
            }

            public override string Description
            {
                get { return _description; }
            }

            public override void Apply(Word word)
            {
                int applied = 0;
                Action<AbstractRule, Word, IWordSlice> ruleApplied = (r, w, s) => 
                {
                    applied++;
                };
                _rs.RuleApplied += ruleApplied;

                try
                {
                    // clear the list of syllables. All of the syllables
                    // constructed during ApplyAll() will get added to the list
                    _list.Clear();

                    OnEntered(word);
                    _rs.ApplyAll(word);

                    SelectOptimalSyllables(_list, word, _direction);

                    if (applied > 0)
                    {
                        OnApplied(word, null);
                    }
                }
                finally
                {
                    _rs.RuleApplied -= ruleApplied;
                    _list.Clear();
                    OnExited(word);
                }
            }
        }

        private IEnumerable<Rule> BuildRuleList(List<Syllable> syllableList)
        {
            var rules = new List<Rule>();

            var activeOnsets = new List<IEnumerable<IMatrixMatcher>>();
            var activeCodas = new List<IEnumerable<IMatrixMatcher>>();

            activeOnsets.AddRange(Onsets);
            activeCodas.AddRange(Codas);

            if (activeOnsets.Count == 0)
            {
                activeOnsets.Add(null);
            }
            if (activeCodas.Count == 0)
            {
                activeCodas.Add(null);
            }
            
            foreach (var onset in activeOnsets)
            {
                foreach (var nucleus in Nuclei)
                {
                    foreach (var coda in activeCodas)
                    {
                        var rule = BuildRule(onset, nucleus, coda, syllableList);
                        rules.Add(rule);
                    }
                }
            }
            Debug.Assert(rules.Count > 0);

            return rules;
        }

        private Rule BuildRule(IEnumerable<IMatrixMatcher> onset, 
                               IEnumerable<IMatrixMatcher> nucleus, 
                               IEnumerable<IMatrixMatcher> coda,
                               List<Syllable> syllableList)
        {
            var list = new List<IRuleSegment>();
            var ctx = new SyllableContext(syllableList);

            list.Add(new SyllableBegin(ctx));

            if (onset != null)
            {
                list.AddRange(onset.Select(onsetSeg => (IRuleSegment) new SyllableElement(onsetSeg, ctx.Onset)));
            }

            Debug.Assert(nucleus != null);
            list.AddRange(nucleus.Select(nucleusSeg => (IRuleSegment) new SyllableElement(nucleusSeg, ctx.Nucleus)));

            if (coda != null)
            {
                list.AddRange(coda.Select(codaSeg => (IRuleSegment) new SyllableElement(codaSeg, ctx.Coda)));
            }

            list.Add(new SyllableEnd(ctx));

            var rule = new Rule("syllable", list, new IRuleSegment[] {});
            rule.Direction = Phonix.Direction.Leftward;

            return rule;
        }

        private string BuildDescription()
        {
            var str = new StringBuilder();
            str.AppendLine("syllable");

            foreach (var onset in Onsets.Where(o => o.Count() > 0))
            {
                str.Append("onset ");
                foreach (var match in onset)
                {
                    str.Append(match.ToString());
                }
                str.AppendLine();
            }

            foreach (var nucleus in Nuclei)
            {
                str.Append("nucleus ");
                foreach (var match in nucleus)
                {
                    str.Append(match.ToString());
                }
                str.AppendLine();
            }

            foreach (var coda in Codas.Where(o => o.Count() > 0))
            {
                str.Append("coda ");
                foreach (var match in coda)
                {
                    str.Append(match.ToString());
                }
                str.AppendLine();
            }

            return str.ToString();
        }

        private static void SelectOptimalSyllables(List<Syllable> list, Word word, NucleusDirection direction)
        {
            var available = new List<Syllable>(list);
            var selected = new List<Syllable>();
            List<Syllable> best = null;

            RecurseSelectSyllables(available, selected, word, direction, ref best);

            // detach everything
            foreach (var segment in word)
            {
                ((MutableSegment) segment).Detach(Tier.Syllable);
            }
            
            Debug.Assert(word.All(segment => !segment.HasAncestor(Tier.Syllable)));

            // reattach all of our selected syllables
            if (best != null)
            {
                best.ForEach(syllable => syllable.BuildSupraSegments());
            }
        }

        private static void RecurseSelectSyllables(List<Syllable> available, List<Syllable> selected, Word word, NucleusDirection direction, ref List<Syllable> best)
        {
            if (available.Count > 0) // still need to select syllables
            {
                var syllable = available[0];

                var availableModified = new List<Syllable>(available);
                availableModified.Remove(syllable);

                // recurse once *without* selecting this syllable
                RecurseSelectSyllables(availableModified, selected, word, direction, ref best);

                // we can only select this syllable if it doesn't overlap an
                // already-selected syllable
                if (!selected.Any(selectedSyllable => selectedSyllable.Overlaps(syllable)))
                {
                    // recurse once *with* this syllable
                    var selectedModified = new List<Syllable>(selected);
                    selectedModified.Add(syllable);
                    RecurseSelectSyllables(availableModified, selectedModified, word, direction, ref best);
                }
            }
            else // evaluate the selected syllables
            {
                best = RankSyllables(best, selected, direction, word);
            }
        }

        private static List<Syllable> RankSyllables(List<Syllable> a, List<Syllable> b, NucleusDirection direction, Word word)
        {
            // if either is null, return the other
            if (a == null || b == null)
            {
                return a ?? b;
            }

            // select based on fewest unsyllabified segments
            int aUnsyllabified = CountUnsyllabified(a, word);
            int bUnsyllabified = CountUnsyllabified(b, word);
            if (aUnsyllabified < bUnsyllabified)
            {
                return a;
            }
            else if (bUnsyllabified < aUnsyllabified)
            {
                return b;
            }

            // select based on fewest number of syllables
            if (a.Count < b.Count)
            {
                return a;
            }
            if (b.Count < a.Count)
            {
                return b;
            }

            // select on alignment, if not neutral
            if (direction != NucleusDirection.Neutral)
            {
                var wordList = new List<Segment>(word);
                int aNucleusSum = a.Aggregate(0, (sum, syllable) => { return sum + wordList.IndexOf(syllable.Nucleus.First()); });
                int bNucleusSum = b.Aggregate(0, (sum, syllable) => { return sum + wordList.IndexOf(syllable.Nucleus.First()); });
                if (direction == NucleusDirection.Left)
                {
                    // select the set with nuclei more to the left (smaller indices)
                    return aNucleusSum < bNucleusSum ? a : b;
                }
                else if (direction == NucleusDirection.Right)
                {
                    // select the set with nuclei more to the right (larger indices)
                    return aNucleusSum > bNucleusSum ? a : b;
                }
            }

            // select based on most onsets
            int aOnsets = a.Aggregate(0, (sum, syllable) => { return sum + syllable.Onset.Count(); });
            int bOnsets = b.Aggregate(0, (sum, syllable) => { return sum + syllable.Onset.Count(); });
            if (aOnsets > bOnsets)
            {
                return a;
            }
            if (bOnsets > aOnsets)
            {
                return b;
            }

            // no choice? return the first one
            return a;
        }

        private static int CountUnsyllabified(List<Syllable> list, Word word)
        {
            int count = 0;

            foreach (var seg in word)
            {
                if (!list.Any(syllable => syllable.Contains(seg)))
                {
                    count++;
                }
            }

            return count;
        }

        private class SyllableContext
        {
            public readonly List<Segment> Onset = new List<Segment>();
            public readonly List<Segment> Nucleus = new List<Segment>();
            public readonly List<Segment> Coda = new List<Segment>();
            public readonly List<Syllable> SyllableList;

            public SyllableContext(List<Syllable> syllableList)
            {
                SyllableList = syllableList;
            }
        }

        // this abstract class provides an implementation of IRuleSegment that
        // the syllable segments use, leaving only Combine and (optionally)
        // Matches as methods that the subclasses need to implement.
        private abstract class SyllableSegment : IRuleSegment 
        {
            public virtual bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                return true;
            }

            public abstract void Combine(RuleContext ctx, MutableSegmentEnumerator segment);

            // these are implemented here for convenience
            public bool IsMatchOnlySegment 
            { 
                get { return false; } 
            }
            public string MatchString 
            { 
                get { return ""; } 
            }
            public string CombineString 
            { 
                get { return ""; } 
            }
        }

        private class SyllableBegin : SyllableSegment
        {
            private readonly SyllableContext _syllCtx;

            public SyllableBegin(SyllableContext syllCtx)
            {
                _syllCtx = syllCtx;
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                // set up the context for the new syllable
                _syllCtx.Onset.Clear();
                _syllCtx.Nucleus.Clear();
                _syllCtx.Coda.Clear();
            }
        }

        private class SyllableEnd : SyllableSegment
        {
            private readonly SyllableContext _syllCtx;

            public SyllableEnd(SyllableContext syllCtx)
            {
                _syllCtx = syllCtx;
            }
            
            public override void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                var syllable = new Syllable(_syllCtx.Onset, _syllCtx.Nucleus, _syllCtx.Coda);
                if (!_syllCtx.SyllableList.Contains(syllable))
                {
                    _syllCtx.SyllableList.Add(syllable);
                }
            }
        }

        private class SyllableElement : SyllableSegment
        {
            private readonly IMatrixMatcher _match;
            private readonly List<Segment> _list;

            public SyllableElement(IMatrixMatcher match, List<Segment> list)
            {
                _match = match;
                _list = list;
            }

            public override bool Matches(RuleContext ctx, SegmentEnumerator seg)
            {
                return seg.MoveNext() && _match.Matches(ctx, seg.Current);
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator seg)
            {
                seg.MoveNext();
                _list.Add(seg.Current);
            }
        }
    }
}
