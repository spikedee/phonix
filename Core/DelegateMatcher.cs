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

        public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            return _match(ctx, matrix);
        }

        override public string ToString()
        {
            return _desc;
        }
    }
}
