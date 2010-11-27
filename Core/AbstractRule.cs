using System;

namespace Phonix
{
    public abstract class AbstractRule
    {
        protected internal AbstractRule(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            Name = name;
        }

        public readonly string Name;

        public abstract string Description
        {
            get;
        }

        public event Action<AbstractRule, Word> Entered;
        public event Action<AbstractRule, Word, WordSlice> Applied;
        public event Action<AbstractRule, Word> Exited;

        protected internal void OnEntered(Word word)
        {
            var entered = Entered;
            if (entered != null)
            {
                entered(this, word);
            }
        }

        protected internal void OnApplied(Word word, WordSlice slice)
        {
            var applied = Applied;
            if (applied != null)
            {
                applied(this, word, slice);
            }
        }

        protected internal void OnExited(Word word)
        {
            var exited = Exited;
            if (exited != null)
            {
                exited(this, word);
            }
        }

        public abstract void Apply(Word word);

        public abstract string ShowApplication(Word word, WordSlice slice, SymbolSet symbolSet);

        public override string ToString()
        {
            return Name;
        }
    }
}
