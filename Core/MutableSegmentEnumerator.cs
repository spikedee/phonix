namespace Phonix
{
    public interface MutableSegmentEnumerator : SegmentEnumerator
    {
        void InsertBefore(FeatureMatrix fm);
        void InsertAfter(FeatureMatrix fm);
        void Delete();
        new FeatureMatrix Current { get; set; }
    }
}
