namespace Phonix
{
    public class ContextSegment : IRuleSegment
    {
        private readonly IMatrixMatcher _match;

        public ContextSegment(IMatrixMatcher match)
        {
            _match = match;
        }

        public virtual bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(ctx, pos.Current))
            {
                return true;
            }
            return false;
        }

        public virtual void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
        }

        public bool IsMatchOnlySegment { get { return true; } }
        public string MatchString { get { return _match.ToString(); } }
        public string CombineString { get { return ""; } }
    }
}
