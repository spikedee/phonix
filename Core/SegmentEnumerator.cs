using System.Collections.Generic;

namespace Phonix
{
    public interface SegmentEnumerator : IEnumerator<Segment>
    {
        bool IsFirst { get; }
        bool IsLast { get; }
        bool MovePrev();
        void Mark();
        void Revert();
        new Segment Current { get; }
    }
}
