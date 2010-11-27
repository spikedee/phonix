namespace Phonix
{
    public abstract class WordBoundary : IRuleSegment
    {
        private WordBoundary()
        {
            // can only be instatiated by private inner classes
        }

        public static readonly WordBoundary Left = new LeftBoundary();
        public static readonly WordBoundary Right = new RightBoundary();

        abstract public bool Matches(RuleContext ctx, SegmentEnumerator segment);

        public void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
        {
            // nothing to do
        }

        public bool IsMatchOnlySegment { get { return true; } }
        public string MatchString { get { return "$"; } }
        public string CombineString { get { return ""; } }

        private class LeftBoundary : WordBoundary
        {
            public LeftBoundary() : base()
            {
            }

            override public bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                segment.MoveNext();
                return !segment.MovePrev();
            }
        }

        private class RightBoundary : WordBoundary
        {
            public RightBoundary() : base()
            {
            }

            override public bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                bool match = !segment.MoveNext();
                segment.MovePrev();
                return match;
            }
        }
    }
}
