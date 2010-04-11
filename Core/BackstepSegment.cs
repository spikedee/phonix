namespace Phonix
{
    public class BackstepSegment : IRuleSegment
    {
        // this class moves the cursor one step backwards, but doesn't actually
        // do anything to the segments.

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            // we only want to return false if we are already before the
            // beginning. This requires us to advance, check IsFirst, then move
            // back

            if (pos.MoveNext() && pos.IsFirst)
            {
                pos.MovePrev();
                return false;
            }
            pos.MovePrev();
            pos.MovePrev();
            return true;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            Matches(ctx, pos);
        }

        public bool IsMatchOnlySegment { get { return true; } }
        public string MatchString { get { return ""; } }
        public string CombineString { get { return ""; } }
    }
}
