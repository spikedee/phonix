namespace Phonix
{
    public class StepSegment : IRuleSegment
    {
        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            return pos.MoveNext();
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
