using System;
using System.Text;
using System.Linq;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class SyllableTest
    {
        private readonly IRuleSegment SegmentA = new ContextSegment(new MatrixMatcher(FeatureMatrixTest.MatrixA));
        private readonly IRuleSegment SegmentB = new ContextSegment(new MatrixMatcher(FeatureMatrixTest.MatrixB));
        private readonly IRuleSegment SegmentC = new ContextSegment(new MatrixMatcher(FeatureMatrixTest.MatrixC));

        private Word GetTestWord()
        {
            var segs = new FeatureMatrix[]
            {
                FeatureMatrixTest.MatrixB,
                FeatureMatrixTest.MatrixC,
                FeatureMatrixTest.MatrixA,
                FeatureMatrixTest.MatrixC,
                FeatureMatrixTest.MatrixB,
            };
            return new Word(segs);
        }

        private string ShowSyllables(Word word)
        {
            StringBuilder str = new StringBuilder();
            Segment lastSyll = null;
            Segment lastSegment = null;
            var symbols = SymbolSetTest.GetTestSet();

            foreach (var segment in word)
            {
                Segment thisSyll;
                if (segment.HasAncestor(Tier.Syllable))
                {
                    var ancestors = segment.FindAncestors(Tier.Syllable);

                    Assert.AreEqual(1, ancestors.Count(),
                            String.Format("{0} is linked to two syllables", symbols.Spell(segment.Matrix)));

                    thisSyll = ancestors.First();
                    if (thisSyll != lastSyll)
                    {
                        if (lastSyll != null)
                        {
                            str.Append(">");
                        }
                        str.Append("<");
                        lastSyll = thisSyll;
                    }
                }
                else
                {
                    if (lastSyll != null)
                    {
                        str.Append(">");
                    }
                    lastSyll = null;
                }

                if (lastSegment != null)
                {
                    if (segment.HasAncestor(Tier.Nucleus) && lastSegment.HasAncestor(Tier.Onset))
                    {
                        str.Append(":");
                    }
                    else if (segment.HasAncestor(Tier.Coda) && lastSegment.HasAncestor(Tier.Nucleus))
                    {
                        str.Append(".");
                    }
                }

                str.Append(symbols.Spell(segment.Matrix));
                lastSegment = segment;
            }
            if (lastSyll != null)
            {
                str.Append(">");
            }

            return str.ToString();
        }

        [Test]
        public void Ctor()
        {
            var syll = new SyllableBuilder();

            Assert.IsNotNull(syll.Onsets);
            Assert.IsNotNull(syll.Nuclei);
            Assert.IsNotNull(syll.Codas);
        }

        [Test]
        public void GetSyllableRule()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentB });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });

            var rule = syll.GetSyllableRule();

            Assert.AreEqual(rule.Name, "syllabify");
            Assert.IsNotNull(rule.Description);
            Console.WriteLine(rule.Description);

            // negative tests
            syll.Nuclei.Clear();
            try
            {
                rule = syll.GetSyllableRule();
                Assert.Fail("should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            }
        }

        [Test]
        public void RuleDescription()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentB });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });

            var rule = syll.GetSyllableRule();

            Assert.IsTrue(rule.Description.Contains("syllable"));
            Assert.IsTrue(rule.Description.Contains("onset"));
            Assert.IsTrue(rule.Description.Contains("nucleus"));
            Assert.IsTrue(rule.Description.Contains("coda"));

            Assert.IsTrue(rule.Description.Contains(SegmentA.CombineString));
            Assert.IsTrue(rule.Description.Contains(SegmentB.CombineString));
            Assert.IsTrue(rule.Description.Contains(SegmentC.CombineString));
        }

        [Test]
        public void CAC()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("b<c:a.c>b", ShowSyllables(word));
        }

        [Test]
        public void ResyllabifyCAC()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            int applied = 0;
            rule.Applied += (r, w, s) => { applied++; };

            rule.Apply(word);
            Assert.AreEqual(1, applied);
            var firstSyll = ShowSyllables(word);

            // now repeat and verify that we didn't change anything
            rule.Apply(word);
            Assert.AreEqual(firstSyll, ShowSyllables(word));
        }

        [Test]
        public void MultiOnsetBCAC()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Onsets.Add(new IRuleSegment[] { SegmentB, SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("<bc:a.c>b", ShowSyllables(word));
        }

        [Test]
        public void MultiCodaCACB()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });
            syll.Codas.Add(new IRuleSegment[] { SegmentC, SegmentB });

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("b<c:a.cb>", ShowSyllables(word));
        }

        [Test]
        public void MultiSyllBCCB()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentB });
            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentB });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentC });

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("<b:c>a<c:b>", ShowSyllables(word));
        }

        [Test]
        public void OverlappedCACCB()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentB });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });
            syll.Codas.Add(new IRuleSegment[] {});

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("b<c:a><c:b>", ShowSyllables(word));
        }

        [Test]
        public void Right()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentA });
            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentC });
            syll.Direction = SyllableBuilder.NucleusDirection.Right;

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("bc<a:c>b", ShowSyllables(word));
        }

        [Test]
        public void Left()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] { SegmentA });
            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentC });
            syll.Direction = SyllableBuilder.NucleusDirection.Left;

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("b<c:a>cb", ShowSyllables(word));
        }

        [Test]
        public void PreferOnset()
        {
            var syll = new SyllableBuilder();

            syll.Onsets.Add(new IRuleSegment[] {});
            syll.Onsets.Add(new IRuleSegment[] { SegmentA });
            syll.Onsets.Add(new IRuleSegment[] { SegmentB });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentC });
            syll.Codas.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentB });
            syll.Codas.Add(new IRuleSegment[] {});

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);
            Assert.AreEqual("<b:c><a:c.b>", ShowSyllables(word));
        }
    }
}
