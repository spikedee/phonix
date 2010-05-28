using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public class Syllable
    {
        public bool RequireOnset = true;
        public bool AllowComplexOnset = false;
        public bool AllowCoda = false;
        public bool AllowComplexCoda = false;

        public int MinSonorityDistance = 0;
        public int MinCodaSonority = 0;
        public int MaxEdgeSonority = int.MaxValue;
        public int MinNucleusSonority = 0;

        public Direction Direction = Direction.Rightward;

        public IEnumerable<FeatureValue> Sonorous = new FeatureValue[] {};

        public void Parse(Word word)
        {
        }

        private class SyllableContext
        {
            public readonly List<Segment> Onset = new List<Segment>();
            public readonly List<Segment> Nucleus = new List<Segment>();
            public readonly List<Segment> Coda = new List<Segment>();
        }

        private abstract class SyllableSegment : IRuleSegment
        {
            protected readonly SyllableContext Syllable; 

            protected SyllableSegment(SyllableContext syll)
            {
                Syllable = syll;
            }

            public abstract bool Matches(RuleContext ctx, SegmentEnumerator segment);
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

        private class SyllableStart : SyllableSegment
        {
            public SyllableStart(SyllableContext syll)
                : base(syll)
            {
            }

            public override bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                try
                {
                    // if the current segment in the iterator is anything other
                    // than an onset, we could potentially start a new syllable
                    // on the next segment
                    return !segment.Current.HasAncestor(Tier.Onset);
                }
                catch (InvalidOperationException)
                {
                    // segment.Current wasn't valid. If this is because the
                    // iteration hasn't yet started, then we still want to
                    // return true.
                    return segment.IsFirst;
                }
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                // set up the context for the new syllable
                Syllable.Onset.Clear();
                Syllable.Nucleus.Clear();
                Syllable.Coda.Clear();
            }
        }

        private class SyllableFinish : SyllableSegment
        {
            public SyllableFinish(SyllableContext syll)
                : base(syll)
            {
            }

            public override bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                return true;
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                MutableSegment syllable;
                MutableSegment onset;
                MutableSegment rime;
                MutableSegment nucleus;
                MutableSegment coda;

                var nuclearSeg = Syllable.Nucleus[0]; // this should *always* exist
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

                onset.Children = Syllable.Onset;
                nucleus.Children = Syllable.Nucleus;
                coda.Children = Syllable.Coda;
            }
        }

        private class SegmentWrapper : SyllableSegment
        {
            private readonly IEnumerable<IRuleSegment> _wrappedSegments;
            private readonly Tier _tier;
            private readonly bool _required;

            private bool _lastMatch;
            private readonly List<Segment> _matchedList = new List<Segment>();

            public SegmentWrapper(SyllableContext syll, IEnumerable<IRuleSegment> wrapped, Tier targetTier, bool required)
                : base(syll)
            {
                _wrappedSegments = wrapped;
                _tier = targetTier;
                _required = required;
            }

            public override bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                _lastMatch = false;
                _matchedList.Clear();

                foreach (var seg in _wrappedSegments)
                {
                    if (seg.Matches(ctx, segment))
                    {
                        // d00d, this be totally hard
                    }
                }
                _lastMatch = true;

                return (_lastMatch || !_required);
            }

            public override void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                if (_tier == Tier.Onset)
                {
                    // TODO
                }
            }
        }
    }
}
