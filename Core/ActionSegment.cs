namespace Phonix
{
    public class ActionSegment : IRuleSegment
    {
        private readonly IMatrixMatcher _match;
        private readonly IMatrixCombiner _combo;

        public ActionSegment(IMatrixMatcher match, IMatrixCombiner combo)
        {
            _match = match;
            _combo = combo;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(ctx, pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Current = _combo.Combine(ctx, pos.Current);
        }

        public bool IsMatchOnlySegment { get { return false; } }
        public string MatchString { get { return _match.ToString(); } }
        public string CombineString { get { return _combo.ToString(); } }
    }
}
