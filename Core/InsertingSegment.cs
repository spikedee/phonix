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
            pos.InsertAfter(_insert.Combine(ctx, FeatureMatrix.Empty));
            pos.MoveNext();
        }

        public bool IsMatchOnlySegment { get { return false; } }
        public string MatchString { get { return "*"; } }
        public string CombineString { get { return _insert.ToString(); } }
    }
}
