using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Phonix
{
    public class Syllable
    {
        public readonly List<IEnumerable<IRuleSegment>> Onsets = new List<IEnumerable<IRuleSegment>>();
        public readonly List<IEnumerable<IRuleSegment>> Nuclei = new List<IEnumerable<IRuleSegment>>();
        public readonly List<IEnumerable<IRuleSegment>> Codas = new List<IEnumerable<IRuleSegment>>();

        public bool OnsetRequired = false;
        public bool OnsetForbidden = false;

        public bool CodaRequired = false;
        public bool CodaForbidden = false;

        public AbstractRule GetSyllableRule()
        {
            ValidateState();

            var rs = new RuleSet();
            var rules = BuildRuleList();

            foreach (var rule in rules)
            {
                rs.Add(rule);
            }

            return new SyllableRule(rs, BuildDescription());
        }

        private void ValidateState()
        {
            if (Nuclei.Count == 0)
            {
                throw new InvalidOperationException("Nucleus list must not be empty");
            }

            if (OnsetRequired)
            {
                if (Onsets.Count == 0)
                {
                    throw new InvalidOperationException("Onsets are required, but the onset list is empty");
                }
                if (OnsetForbidden)
                {
                    throw new InvalidOperationException("Onsets cannot be both required and forbidden");
                }
            }
            else if (OnsetForbidden)
            {
                if (Onsets.Count != 0)
                {
                    throw new InvalidOperationException("Onsets are forbidden but the onset list is not empty");
                }
            }

            if (CodaRequired)
            {
                if (Codas.Count == 0)
                {
                    throw new InvalidOperationException("Codas are required, but the coda list is empty");
                }
                if (CodaForbidden)
                {
                    throw new InvalidOperationException("Codas cannot be both required and forbidden");
                }
            }
            else if (CodaForbidden)
            {
                if (Codas.Count != 0)
                {
                    throw new InvalidOperationException("Codas are forbidden but the coda list is not empty");
                }
            }
        }

        private class SyllableRule : AbstractRule
        {
            private RuleSet _rs;
            private string _description;

            public SyllableRule(RuleSet rs, string description)
                : base ("syllabify")
            {
                _rs = rs;
                _description = description;
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
                    OnEntered(word);
                    _rs.ApplyAll(word);

                    if (applied > 0)
                    {
                        OnApplied(word, null);
                    }
                }
                finally
                {
                    _rs.RuleApplied -= ruleApplied;
                    OnExited(word);
                }
            }
        }

        private IEnumerable<Rule> BuildRuleList()
        {
            var rules = new List<Rule>();
            
            if (!OnsetRequired && Onsets.Count == 0)
            {
                Onsets.Add(new IRuleSegment[] {});
            }
            if (!CodaRequired && Codas.Count == 0)
            {
                Codas.Add(new IRuleSegment[] {});
            }

            foreach (var onset in Onsets)
            {
                foreach (var nucleus in Nuclei)
                {
                    foreach (var coda in Codas)
                    {
                        var rule = BuildRule(onset, nucleus, coda);
                        rules.Add(rule);
                    }
                }
            }

            // Sort the rules we've built with so that the longest is first
            Debug.Assert(rules.Count > 0);
            rules.Sort((a, b) => b.Segments.Count().CompareTo(a.Segments.Count()));

            return rules;
        }

        private Rule BuildRule(IEnumerable<IRuleSegment> onset, 
                               IEnumerable<IRuleSegment> nucleus, 
                               IEnumerable<IRuleSegment> coda)
        {
            var list = new List<IRuleSegment>();
            var ctx = new SyllableContext();

            list.Add(new SyllableBegin(ctx));

            if (onset.Count() > 0)
            {
                list.Add(new TierBegin(ctx));
                list.AddRange(onset);
                list.Add(new TierEnd(ctx, ctx.Onset));
            }

            list.Add(new TierBegin(ctx));
            list.AddRange(nucleus);
            list.Add(new TierEnd(ctx, ctx.Nucleus));

            if (coda.Count() > 0)
            {
                list.Add(new TierBegin(ctx));
                list.AddRange(coda);
                list.Add(new TierEnd(ctx, ctx.Coda));
            }

            list.Add(new SyllableEnd(ctx));

            return new Rule("syllable", list, new IRuleSegment[] {});
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

        private class SyllableContext
        {
            public readonly List<Segment> Onset = new List<Segment>();
            public readonly List<Segment> Nucleus = new List<Segment>();
            public readonly List<Segment> Coda = new List<Segment>();
            public SegmentEnumerator.Marker BeginMark;
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

            public override bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                // if the current segment in the iterator is anything other
                // than an onset, we could potentially start a new syllable on
                // the next segment.

                bool rv = segment.MoveNext() && !segment.Current.HasAncestor(Tier.Onset);
                if (rv)
                {
                    // we don't actually want to consume the next segment, so we move back
                    segment.MovePrev();
                }
                return rv;
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
                MutableSegment syllable;
                MutableSegment onset;
                MutableSegment rime;
                MutableSegment nucleus;
                MutableSegment coda;

                var nuclearSeg = SyllableCtx.Nucleus[0]; // this should *always* exist
                if (nuclearSeg.HasAncestor(Tier.Nucleus))
                {
                    // get the existing syllable elements to use here
                    syllable = (MutableSegment) nuclearSeg.FindAncestor(Tier.Syllable);
                    rime = (MutableSegment) nuclearSeg.FindAncestor(Tier.Rime);
                    nucleus = (MutableSegment) nuclearSeg.FindAncestor(Tier.Nucleus);
                    onset = (MutableSegment) syllable.FindDescendant(Tier.Onset);
                    coda = (MutableSegment) syllable.FindDescendant(Tier.Coda);
                }
                else
                {
                    // build a new syllable structure
                    onset = new MutableSegment(Tier.Onset);
                    nucleus = new MutableSegment(Tier.Nucleus);
                    coda = new MutableSegment(Tier.Coda);
                    rime = new MutableSegment(Tier.Rime, FeatureMatrix.Empty, new Segment[] { nucleus, coda });
                    syllable = new MutableSegment(Tier.Syllable, FeatureMatrix.Empty, new Segment[] { onset, rime });
                }

                onset.Children = SyllableCtx.Onset;
                nucleus.Children = SyllableCtx.Nucleus;
                coda.Children = SyllableCtx.Coda;
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
                SyllableCtx.BeginMark = seg.Mark();
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
                var endMark = seg.Mark();
                _list.AddRange(seg.Span(SyllableCtx.BeginMark, endMark));
            }
        }
    }
}
