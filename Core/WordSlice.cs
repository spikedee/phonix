namespace Phonix
{
    public interface IWordSlice
    {
        SegmentEnumerator GetEnumerator();
        MutableSegmentEnumerator GetMutableEnumerator();
    }
}
