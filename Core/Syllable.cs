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

        public RuleSet GetRuleSet()
        {
            var rs = new RuleSet();

            // create the segments for nuclei and onsets
            var listCore = new List<IRuleSegment>();
            listCore.Add(new AssignNucleus(this, true));
            listCore.Add(new AssignOnset(this, RequireOnset));
            if (AllowComplexOnset)
            {
                listCore.Add(new AssignComplexOnset(this, false));
            }

            var coreRule = new Rule("syllable-core", listCore, new IRuleSegment[] {});
            coreRule.Direction = Direction;
            rs.Add(coreRule);

            // create the segments for codas
            if (AllowCoda)
            {
                var listCoda = new List<IRuleSegment>();
                listCoda.Add(new IsNucleus(this, true));
                listCoda.Add(new AssignCoda(this, true));
                if (AllowComplexCoda)
                {
                    listCoda.Add(new AssignComplexCoda(this, false));
                }

                var codaRule = new Rule("syllable-coda", listCoda, new IRuleSegment[] {});
                rs.Add(codaRule);
            }

            return rs;
        }

        private int Sonority(Segment seg)
        {
            int sonority = 0;
            foreach (var fv in Sonorous)
            {
                if (seg.Matrix[fv.Feature] == fv)
                {
                    sonority++;
                }
            }
            return sonority;
        }

        private abstract class SyllableSegment : IRuleSegment
        {
            protected readonly Syllable _syllable;
            private readonly bool _required;
            private bool _lastMatched;

            public SyllableSegment(Syllable syllable, bool required)
            {
                _syllable = syllable;
                _required = required;
            }

            public bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                _lastMatched = MatchesImpl(ctx, segment);
                return  _lastMatched || !_required;
            }

            public void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                if (_lastMatched)
                {
                    CombineImpl(ctx, segment);
                }
            }

            // these are implemented here for convenience
            public bool IsMatchOnlySegment { get { return false; } }
            public string MatchString { get { return ""; } }
            public string CombineString { get { return ""; } }

            // these need to be implemented by subclasses
            protected abstract bool MatchesImpl(RuleContext ctx, SegmentEnumerator segment);
            protected abstract void CombineImpl(RuleContext ctx, MutableSegmentEnumerator segment);
        }

        private class AssignNucleus : SyllableSegment
        {
            public AssignNucleus(Syllable syllable, bool required)
                : base(syllable, required) {}

            override protected bool MatchesImpl(RuleContext ctx, SegmentEnumerator segment)
            {
                if (!segment.MoveNext())
                {
                    return false;
                }

                var son = _syllable.Sonority(segment.Current);
                if (son > _syllable.MaxEdgeSonority)
                {
                    // too sonorant to be an edge
                    return true;
                } 
                else if (son < _syllable.MinNucleusSonority)
                {
                    // not sonorant enough to be a nucleus
                    return false;
                }

                // is this segment less sonorous than the previous segment?
                if (segment.MovePrev())
                {
                    if (son < _syllable.Sonority(segment.Current))
                    {
                        return false;
                    }
                }
                segment.MoveNext();

                // is this segment less sonorous than the following segment?
                if (segment.MoveNext())
                {
                    if (son < _syllable.Sonority(segment.Current))
                    {
                        return false;
                    }
                }
                segment.MovePrev();

                // getting here implies that this is a sonority peak
                return true;
            }

            override protected void CombineImpl(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                segment.MoveNext();
                if (!segment.Current.HasAncestor(Tier.Nucleus))
                {
#pragma warning disable 168
                    segment.Current.Detach();

                    var nucleus = new MutableSegment(Tier.Nucleus, FeatureMatrix.Empty, new Segment[] { segment.Current });
                    var rime = new MutableSegment(Tier.Rime, FeatureMatrix.Empty, new Segment[] { nucleus });
                    var syllable = new MutableSegment(Tier.Syllable, FeatureMatrix.Empty, new Segment[] { rime });
#pragma warning restore 168
                }
            }
        }

        private class AssignOnset : SyllableSegment
        {
            public AssignOnset(Syllable syllable, bool required)
                : base(syllable, required) {}

            override protected bool MatchesImpl(RuleContext ctx, SegmentEnumerator segment)
            {
                return segment.MovePrev()
                    && !segment.Current.HasAncestor(Tier.Nucleus)
                    && _syllable.Sonority(segment.Current) <= _syllable.MaxEdgeSonority;
            }

            override protected void CombineImpl(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                var syllable = (MutableSegment) segment.Current.FindAncestor(Tier.Syllable);
                var rime = syllable.Children.First(seg => seg.Tier == Tier.Rime);

                segment.MovePrev();
                if (segment.Current.HasAncestor(Tier.Onset) 
                        && segment.Current.FindAncestor(Tier.Syllable) == syllable)
                {
                    // we're already joined to the appropriate syllable, so do nothing
                    return;
                }

                segment.Current.Detach();
                var onset = new MutableSegment(Tier.Onset, FeatureMatrix.Empty, new Segment[] { segment.Current });
                syllable.Children = new Segment[] { onset, rime };
            }
        }

        private class AssignComplexOnset : SyllableSegment
        {
            public AssignComplexOnset(Syllable syllable, bool required)
                : base(syllable, required) {}

            override protected bool MatchesImpl(RuleContext ctx, SegmentEnumerator segment)
            {
                var nextSeg = segment.Current;

                return segment.MovePrev()
                    && !segment.Current.HasAncestor(Tier.Syllable)
                    && _syllable.Sonority(segment.Current) <= _syllable.MaxEdgeSonority
                    && (_syllable.Sonority(nextSeg) - _syllable.Sonority(segment.Current)) >= _syllable.MinSonorityDistance;
            }

            override protected void CombineImpl(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                var segs = new List<Segment>();
                var onset = (MutableSegment) segment.Current.FindAncestor(Tier.Onset);

                segment.MovePrev();
                segs.Add(segment.Current);
                segs.AddRange(onset.Children);

                onset.Children = segs;
            }
        }

        private class IsNucleus : SyllableSegment
        {
            public IsNucleus(Syllable syllable, bool required)
                : base(syllable, required) {}

            override protected bool MatchesImpl(RuleContext ctx, SegmentEnumerator segment)
            {
                return segment.MoveNext()
                    && segment.Current.HasAncestor(Tier.Nucleus);
            }

            override protected void CombineImpl(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                // nothing
            }
        }

        private class AssignCoda : SyllableSegment
        {
            public AssignCoda(Syllable syllable, bool required)
                : base(syllable, required) {}

            override protected bool MatchesImpl(RuleContext ctx, SegmentEnumerator segment)
            {
                return segment.MoveNext()
                    && !segment.Current.HasAncestor(Tier.Onset)
                    && _syllable.Sonority(segment.Current) <= _syllable.MaxEdgeSonority
                    && _syllable.Sonority(segment.Current) >= _syllable.MinCodaSonority;
            }

            override protected void CombineImpl(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                var rime = (MutableSegment) segment.Current.FindAncestor(Tier.Rime);

                segment.MoveNext();
                if (segment.Current.HasAncestor(Tier.Coda) 
                        && segment.Current.FindAncestor(Tier.Rime) == rime)
                {
                    // we're already joined to the appropriate syllable
                    return;
                }

                var coda = new MutableSegment(Tier.Coda, FeatureMatrix.Empty, new Segment[] { segment.Current });

                var list = new List<Segment>( rime.Children );
                list.Add(coda);

                rime.Children = list;
            }
        }

        private class AssignComplexCoda : SyllableSegment
        {
            public AssignComplexCoda(Syllable syllable, bool required)
                : base(syllable, required) {}

            override protected bool MatchesImpl(RuleContext ctx, SegmentEnumerator segment)
            {
                return segment.MoveNext()
                    && !segment.Current.HasAncestor(Tier.Onset)
                    && _syllable.Sonority(segment.Current) <= _syllable.MaxEdgeSonority
                    && _syllable.Sonority(segment.Current) >= _syllable.MinCodaSonority;
            }

            override protected void CombineImpl(RuleContext ctx, MutableSegmentEnumerator segment)
            {
                var coda = (MutableSegment) segment.Current.FindAncestor(Tier.Coda);

                segment.MoveNext();

                var list = new List<Segment>( coda.Children );
                list.Add( segment.Current );

                coda.Children = list;
            }
        }
    }
}
