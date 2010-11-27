using System;

namespace Phonix
{
    public class WordSlice
    {
        private readonly IMatrixMatcher _filter;
        private readonly Word.Node _node;

        internal WordSlice(Word.Node node, IMatrixMatcher filter)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            _node = node;
            _filter = filter;
        }

        public MutableSegmentEnumerator GetMutableEnumerator()
        {
            return new MutableSegmentEnumerator(_node, _filter);
        }

        public SegmentEnumerator GetEnumerator()
        {
            return GetMutableEnumerator();
        }
    }
}
