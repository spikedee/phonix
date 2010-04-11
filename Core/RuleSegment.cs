namespace Phonix
{
    public interface IRuleSegment
    {

        /* This function should return true if the implementation matches the
         * segment(s) at the next position(s) of the enumeration. When this is
         * called, the iterator will be at the start of the enumeration or on
         * the last segment matched by the previous IRuleSegment, so
         * implementations should call segment.MoveNext() (depending on how
         * many segments from the input they consume) _before_ testing
         * segment.Current.
         */
        bool Matches(RuleContext ctx, SegmentEnumerator segment);

        /* This function consumes zero or more segments from the enumeration,
         * modifying them as appropriate. The directions about MoveNext()
         * mentioned for Matches also applies here.
         */
         void Combine(RuleContext ctx, MutableSegmentEnumerator segment); 

        /* If this returns TRUE, then the implementation warrants that it does
         * not modify any segments in Combine(), and only moves the position of
         * the enumerator. This is only used for metadata and reporting
         */
         bool IsMatchOnlySegment { get; }

         string MatchString { get; }
         string CombineString { get; }
    }
}
