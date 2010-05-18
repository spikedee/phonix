using System.Collections.Generic;

namespace Phonix
{
    public interface MutableSegmentEnumerator : SegmentEnumerator, IEnumerator<MutableSegment>
    {
        void InsertBefore(MutableSegment segment);
        void InsertAfter(MutableSegment segment);
        void Delete();
        new MutableSegment Current { get; }
    }
}
