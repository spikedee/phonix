namespace Phonix
{
    internal class DelegateMatcher : IMatchable
    {
        public delegate bool MatchFunc(RuleContext ctx, FeatureMatrix fm);
        private readonly MatchFunc _match;
        private readonly string _desc;

        public DelegateMatcher(MatchFunc match, string desc)
        {
            _match = match;
            _desc = desc;
        }

        public bool Matches(RuleContext ctx, Segment segment)
        {
            return _match(ctx, segment.Matrix);
        }

        override public string ToString()
        {
            return _desc;
        }
    }
}
