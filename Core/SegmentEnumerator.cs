using System.Collections.Generic;

namespace Phonix
{
    public interface SegmentEnumerator : IEnumerator<FeatureMatrix>
    {
        bool IsFirst { get; }
        bool IsLast { get; }
        bool MovePrev();
        new FeatureMatrix Current { get; }
        void Mark();
        void Revert();
    }
}
