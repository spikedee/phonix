using System;

namespace Phonix
{
    public partial class Word
    {
        internal class Node
        {
            internal Node(Word word, MutableSegment segment)
            {
                if (word == null)
                {
                    throw new ArgumentNullException("word");
                }

                Word = word;
                Segment = segment;
            }

            private MutableSegment _segment;

            internal MutableSegment Segment
            {
                get 
                { 
                    return _segment; 
                }
                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("segment value");
                    }
                    _segment = value;
                }
            }

            internal Node Next
            {
                get;
                private set;
            }

            internal Node Prev
            {
                get;
                private set;
            }

            internal readonly Word Word;

            internal bool Deleted
            {
                get;
                private set;
            }

            public void Delete()
            {
                if (Next != null)
                {
                    Next.Prev = this.Prev;
                }
                if (Prev != null)
                {
                    Prev.Next = this.Next;
                }

                if (Word.First == this)
                {
                    Word.First = this.Next;
                }
                else if (Word.Last == this)
                {
                    Word.Last = this.Prev;
                }

                Deleted = true;
            }

            public void InsertAfter(MutableSegment newSeg)
            {
                if (newSeg == null)
                {
                    throw new ArgumentNullException("inserted");
                }
                if (Deleted)
                {
                    throw new SegmentDeletedException();
                }

                var inserted = new Node(Word, newSeg);

                if (this.Next != null)
                {
                    this.Next.Prev = inserted;
                }
                else
                {
                    this.Word.Last = inserted;
                }
                inserted.Next = this.Next;

                this.Next = inserted;
                inserted.Prev = this;
            }

            public void InsertBefore(MutableSegment newSeg)
            {
                if (newSeg == null)
                {
                    throw new ArgumentNullException("inserted");
                }
                if (Deleted)
                {
                    throw new SegmentDeletedException();
                }

                var inserted = new Node(Word, newSeg);

                if (this.Prev != null)
                {
                    this.Prev.Next = inserted;
                }
                else
                {
                    this.Word.First = inserted;
                }
                inserted.Prev = this.Prev;

                this.Prev = inserted;
                inserted.Next = this;
            }
        }
    }
}
