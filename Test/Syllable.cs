using System;

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

        [Test]
        public void Ctor()
        {
            var syll = new Syllable();

            Assert.IsNotNull(syll.Onsets);
            Assert.IsNotNull(syll.Nuclei);
            Assert.IsNotNull(syll.Codas);

            Assert.IsFalse(syll.OnsetRequired);
            Assert.IsFalse(syll.OnsetForbidden);

            Assert.IsFalse(syll.CodaRequired);
            Assert.IsFalse(syll.CodaForbidden);
        }

        [Test]
        public void GetSyllableRule()
        {
            var syll = new Syllable();

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

            syll.OnsetForbidden = true;
            try
            {
                rule = syll.GetSyllableRule();
                Assert.Fail("should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                syll.OnsetForbidden = false;
            }

            syll.CodaForbidden = true;
            try
            {
                rule = syll.GetSyllableRule();
                Assert.Fail("should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                syll.CodaForbidden = false;
            }

            syll.OnsetRequired = true;
            syll.Onsets.Clear();
            try
            {
                rule = syll.GetSyllableRule();
                Assert.Fail("should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                syll.OnsetRequired = false;
                syll.Onsets.Add(new IRuleSegment[] { SegmentB });
            }

            syll.CodaRequired = true;
            syll.Codas.Clear();
            try
            {
                rule = syll.GetSyllableRule();
                Assert.Fail("should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                syll.CodaRequired = false;
                syll.Codas.Add(new IRuleSegment[] { SegmentC });
            }

            syll.OnsetRequired = true;
            syll.OnsetForbidden = true;
            try
            {
                rule = syll.GetSyllableRule();
                Assert.Fail("should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                syll.OnsetRequired = false;
                syll.OnsetForbidden = false;
            }

            syll.CodaRequired = true;
            syll.CodaForbidden = true;
            try
            {
                rule = syll.GetSyllableRule();
                Assert.Fail("should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                syll.CodaRequired = false;
                syll.CodaForbidden = false;
            }
        }

        [Test]
        public void RuleDescription()
        {
            var syll = new Syllable();

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
        public void CVC()
        {
            var syll = new Syllable();

            syll.Onsets.Add(new IRuleSegment[] { SegmentC });
            syll.Nuclei.Add(new IRuleSegment[] { SegmentA });
            syll.Codas.Add(new IRuleSegment[] { SegmentC });

            var rule = syll.GetSyllableRule();
            var word = GetTestWord();

            bool applied = false;
            rule.Applied += (r, w, s) => { applied = true; };
            rule.Apply(word);

            Assert.IsTrue(applied);

            var seg = word.GetEnumerator();

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixB);
            Assert.IsFalse(seg.Current.HasAncestor(Tier.Syllable));

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixC);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Onset));
            var syllSegment = seg.Current.FindAncestor(Tier.Syllable);

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixA);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Rime));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Nucleus));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            var rimeSegment = seg.Current.FindAncestor(Tier.Rime);

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixC);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Rime));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Coda));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            Assert.AreSame(rimeSegment, seg.Current.FindAncestor(Tier.Rime));

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixB);
            Assert.IsFalse(seg.Current.HasAncestor(Tier.Syllable));

            Assert.IsFalse(seg.MoveNext());
        }

        [Test]
        public void MultiOnsetCVC()
        {
            var syll = new Syllable();

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

            var seg = word.GetEnumerator();

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixB);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Onset));
            var syllSegment = seg.Current.FindAncestor(Tier.Syllable);
            var onsetSegment = seg.Current.FindAncestor(Tier.Onset);

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixC);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Onset));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            Assert.AreSame(onsetSegment, seg.Current.FindAncestor(Tier.Onset));

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixA);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Rime));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Nucleus));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            var rimeSegment = seg.Current.FindAncestor(Tier.Rime);

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixC);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Rime));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Coda));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            Assert.AreSame(rimeSegment, seg.Current.FindAncestor(Tier.Rime));

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixB);
            Assert.IsFalse(seg.Current.HasAncestor(Tier.Syllable));

            Assert.IsFalse(seg.MoveNext());
        }

        [Test]
        public void MultiCodaCVC()
        {
            var syll = new Syllable();

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

            var seg = word.GetEnumerator();

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixB);
            Assert.IsFalse(seg.Current.HasAncestor(Tier.Syllable));

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixC);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Onset));
            var syllSegment = seg.Current.FindAncestor(Tier.Syllable);

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixA);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Rime));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Nucleus));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            var rimeSegment = seg.Current.FindAncestor(Tier.Rime);

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixC);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Rime));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Coda));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            Assert.AreSame(rimeSegment, seg.Current.FindAncestor(Tier.Rime));
            var codaSegment = seg.Current.FindAncestor(Tier.Coda);

            Assert.IsTrue(seg.MoveNext());
            Assert.AreSame(seg.Current.Matrix, FeatureMatrixTest.MatrixB);
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Syllable));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Rime));
            Assert.IsTrue(seg.Current.HasAncestor(Tier.Coda));
            Assert.AreSame(syllSegment, seg.Current.FindAncestor(Tier.Syllable));
            Assert.AreSame(rimeSegment, seg.Current.FindAncestor(Tier.Rime));
            Assert.AreSame(codaSegment, seg.Current.FindAncestor(Tier.Coda));

            Assert.IsFalse(seg.MoveNext());
        }
    }
}
