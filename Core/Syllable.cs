using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    internal class Syllable : IEnumerable<Segment>
    {
        private readonly List<Segment> _onset = new List<Segment>();
        private readonly List<Segment> _nucleus = new List<Segment>();
        private readonly List<Segment> _coda = new List<Segment>();

        private readonly Segment _firstSegment;
        private readonly Segment _lastSegment;

        public Syllable(IEnumerable<Segment> onset, IEnumerable<Segment> nucleus, IEnumerable<Segment> coda)
        {
            _onset.AddRange(onset);
            _nucleus.AddRange(nucleus);
            _coda.AddRange(coda);

            _firstSegment = this.First();
            _lastSegment = this.Last();
        }

        public IEnumerable<Segment> Onset
        {
            get { return _onset; }
        }

        public IEnumerable<Segment> Nucleus
        {
            get { return _nucleus; }
        }

        public IEnumerable<Segment> Coda
        {
            get { return _coda; }
        }

        public bool Overlaps(Syllable other)
        {

            // we only check the *first* and the *last* here. This assumes that
            // Syllables checked for overlap are always contiguous parts of the
            // same Word. If that assumption ever isn't true, this will not be
            // a valid test

            return other.Contains(_firstSegment) || other.Contains(_lastSegment);
        }

        public override bool Equals(object other)
        {
            var otherSyllable = other as Syllable;
            if (otherSyllable != null)
            {
                return this.Onset.SequenceEqual(otherSyllable.Onset)
                    && this.Nucleus.SequenceEqual(otherSyllable.Nucleus)
                    && this.Coda.SequenceEqual(otherSyllable.Coda);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this.Aggregate(0, (hash, segment) => { return hash ^ segment.GetHashCode(); });
        }

        public void BuildSupraSegments()
        {
            // detach all of our children (have to make a temporary copy)
            foreach (var seg in this)
            {
                ((MutableSegment) seg).Detach(Tier.Syllable);
            }

#pragma warning disable 168 // unused variable
            var onset = new Segment(Tier.Onset, FeatureMatrix.Empty, Onset);
            var nucleus = new Segment(Tier.Nucleus, FeatureMatrix.Empty, Nucleus);
            var coda = new Segment(Tier.Coda, FeatureMatrix.Empty, Coda);
            var rime = new Segment(Tier.Rime, FeatureMatrix.Empty, new Segment[] { nucleus, coda });
            var syllable = new Segment(Tier.Syllable, FeatureMatrix.Empty, new Segment[] { onset, rime });
        }

        public IEnumerator<Segment> GetEnumerator()
        {
            foreach (var seg in Onset)
            {
                yield return seg;
            }
            foreach (var seg in Nucleus)
            {
                yield return seg;
            }
            foreach (var seg in Coda)
            {
                yield return seg;
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
