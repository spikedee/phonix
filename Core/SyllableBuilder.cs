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
            Left,
            Right
        }

        public readonly List<IEnumerable<IRuleSegment>> Onsets = new List<IEnumerable<IRuleSegment>>();
        public readonly List<IEnumerable<IRuleSegment>> Nuclei = new List<IEnumerable<IRuleSegment>>();
        public readonly List<IEnumerable<IRuleSegment>> Codas = new List<IEnumerable<IRuleSegment>>();
        public NucleusDirection Direction = NucleusDirection.Left;

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

            var activeOnsets = new List<IEnumerable<IRuleSegment>>();
            var activeCodas = new List<IEnumerable<IRuleSegment>>();

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

        private Rule BuildRule(IEnumerable<IRuleSegment> onset, 
                               IEnumerable<IRuleSegment> nucleus, 
                               IEnumerable<IRuleSegment> coda,
                               List<Syllable> syllableList)
        {
            var list = new List<IRuleSegment>();
            var ctx = new SyllableContext(syllableList);

            if (onset != null)
            {
                list.Add(new TierBegin(ctx));
                list.AddRange(onset);
                list.Add(new TierEnd(ctx, ctx.Onset));
            }

            Debug.Assert(nucleus != null);
            list.Add(new TierBegin(ctx));
            list.AddRange(nucleus);
            list.Add(new TierEnd(ctx, ctx.Nucleus));

            if (coda != null)
            {
                list.Add(new TierBegin(ctx));
                list.AddRange(coda);
                list.Add(new TierEnd(ctx, ctx.Coda));
            }

            list.Add(new SyllableEnd(ctx));

            var rule = new Rule("syllable", list, new IRuleSegment[] {});

            return rule;
        }

        private string BuildDescription()
        {
            var str = new StringBuilder();
            str.Append("syllable ");

            foreach (var onset in Onsets)
            {
                str.Append("onset ");
                foreach (var rs in onset)
                {
                    str.Append(rs.MatchString);
                }
                str.AppendLine();
            }

            foreach (var nucleus in Nuclei)
            {
                str.Append("nucleus ");
                foreach (var rs in nucleus)
                {
                    str.Append(rs.MatchString);
                }
                str.AppendLine();
            }

            foreach (var coda in Codas)
            {
                str.Append("coda ");
                foreach (var rs in coda)
                {
                    str.Append(rs.MatchString);
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

            // select on alignment
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
            public SegmentEnumerator.Marker TierBeginMark;
            public readonly List<Syllable> SyllableList;

            public SyllableContext(List<Syllable> syllableList)
            {
                SyllableList = syllableList;
            }
        }

        private abstract class SyllableSegment : IRuleSegment
        {
            protected readonly SyllableContext SyllableCtx;

            protected SyllableSegment(SyllableContext syll)
            {
                SyllableCtx = syll;
            }

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
            public SyllableBegin(SyllableContext syll)
                : base(syll)
            {
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                // set up the context for the new syllable
                SyllableCtx.Onset.Clear();
                SyllableCtx.Nucleus.Clear();
                SyllableCtx.Coda.Clear();
            }
        }

        private class SyllableEnd : SyllableSegment
        {
            public SyllableEnd(SyllableContext syll)
                : base(syll)
            {
            }
            
            public override void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                var syllable = new Syllable(SyllableCtx.Onset, SyllableCtx.Nucleus, SyllableCtx.Coda);
                SyllableCtx.SyllableList.Add(syllable);
            }
        }

        private class TierBegin : SyllableSegment
        {
            public TierBegin(SyllableContext syll)
                : base(syll)
            {
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator seg)
            {
                SyllableCtx.TierBeginMark = seg.Mark();
            }
        }

        private class TierEnd : SyllableSegment
        {
            private readonly List<Segment> _list;

            public TierEnd(SyllableContext syll, List<Segment> list)
                : base(syll)
            {
                _list = list;
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator seg)
            {
                // TODO: this doesn't work because of a bug in Span(). Fix that bug, add a test, then come back to this
                var endMark = seg.Mark();
                _list.AddRange(seg.Span(SyllableCtx.TierBeginMark, endMark));
            }
        }
    }
}
