namespace Phonix
{
    public class Diacritic : Symbol
    {
        // This is an empty subclass, used only to enforce the distinction
        // between Diacritics and Symbols where necessary
        
        public Diacritic(string label, FeatureMatrix fm)
            : base(label, fm)
        {
        }
    }
}
