#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;

namespace Kajitaharuka.EditorLocalization.Tests
{
    [TestFixture]
    public sealed class EditorL10nLocaleTests
    {
        [TestCase("en", "en")]
        [TestCase("EN", "en")]
        [TestCase("en_us", "en-US")]
        [TestCase("zh-hant", "zh-Hant")]
        [TestCase("pt_BR", "pt-BR")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("   ", "")]
        public void NormalizeLocaleTag_NormalizesSupportedTagShapes(string locale, string expected)
        {
            Assert.AreEqual(expected, EditorL10n.NormalizeLocaleTag(locale));
        }

        [Test]
        public void EnumerateLocaleAndParents_ReturnsLocaleThenParents()
        {
            CollectionAssert.AreEqual(
                new[] { "es-419", "es" },
                EditorL10n.EnumerateLocaleAndParents("es-419").ToArray());

            CollectionAssert.AreEqual(
                new[] { "zh-Hant", "zh" },
                EditorL10n.EnumerateLocaleAndParents("zh-Hant").ToArray());

            CollectionAssert.AreEqual(
                new[] { "en" },
                EditorL10n.EnumerateLocaleAndParents("en").ToArray());
        }

        [Test]
        public void EnumerateLocaleAndParents_ReturnsEmptyForBlankLocale()
        {
            CollectionAssert.IsEmpty(EditorL10n.EnumerateLocaleAndParents("").ToArray());
        }

        [Test]
        public void BuildFallbackChain_ReturnsSelectedParentsThenDefault()
        {
            CollectionAssert.AreEqual(
                new[] { "es-419", "es", "ja" },
                EditorL10n.BuildFallbackChain("es-419", "ja").ToArray());
        }

        [Test]
        public void BuildFallbackChain_RemovesDuplicateLocales()
        {
            CollectionAssert.AreEqual(
                new[] { "es-419", "es" },
                EditorL10n.BuildFallbackChain("es-419", "es").ToArray());
        }

        [Test]
        public void BuildFallbackChain_ReturnsDefaultChainWhenSelectedLocaleIsBlank()
        {
            CollectionAssert.AreEqual(
                new[] { "zh-Hant", "zh" },
                EditorL10n.BuildFallbackChain("", "zh-Hant").ToArray());
        }
    }
}
#endif
