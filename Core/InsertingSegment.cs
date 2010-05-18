namespace Phonix
{
    public class InsertingSegment : IRuleSegment
    {
        private IMatrixCombiner _insert;

        public InsertingSegment(IMatrixCombiner insert)
        {
            _insert = insert;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            // always return true, but take nothing from the input list
            return true;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            var segment = new MutableSegment(Tier.Segment, FeatureMatrix.Empty, new Segment[] {});
            _insert.Combine(ctx, segment);

            pos.InsertAfter(segment);
            pos.MoveNext();
        }

        public bool IsMatchOnlySegment { get { return false; } }
        public string MatchString { get { return "*"; } }
        public string CombineString { get { return _insert.ToString(); } }
    }
}
