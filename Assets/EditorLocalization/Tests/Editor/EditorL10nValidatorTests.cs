#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Kajitaharuka.EditorLocalization.Tests
{
    [TestFixture]
    public sealed class EditorL10nValidatorTests
    {
        [Test]
        public void ExtractPlaceholders_ReturnsFormatArgumentNumbers()
        {
            CollectionAssert.AreEquivalent(
                new[] { "0", "1" },
                EditorL10nValidator.ExtractPlaceholders("a {0} b {1}"));

            CollectionAssert.AreEquivalent(
                new[] { "1" },
                EditorL10nValidator.ExtractPlaceholders("{{0}} {1}"));

            CollectionAssert.AreEquivalent(
                new[] { "0" },
                EditorL10nValidator.ExtractPlaceholders("{0:N2}"));

            CollectionAssert.IsEmpty(EditorL10nValidator.ExtractPlaceholders("placeholderなし"));
        }

        [Test]
        public void FindMissingPlaceholderNumbers_ReturnsSequenceGaps()
        {
            CollectionAssert.IsEmpty(EditorL10nValidator.FindMissingPlaceholderNumbers(new[] { "0", "1" }));
            CollectionAssert.AreEqual(
                new[] { 0 },
                EditorL10nValidator.FindMissingPlaceholderNumbers(new[] { "1" }));
            CollectionAssert.AreEqual(
                new[] { 1 },
                EditorL10nValidator.FindMissingPlaceholderNumbers(new[] { "0", "2" }));
        }

        [Test]
        public void ValidateScope_AddsWarningForPlaceholderSequenceGap()
        {
            var result = ValidateSampleScope(
                new Dictionary<string, string> { ["sample.count"] = "項目 {1}" },
                new Dictionary<string, string> { ["sample.count"] = "Items {1}" });

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings.Any(warning => warning.Contains("placeholder番号が連続していません")), Is.True);
            Assert.That(result.Warnings.Any(warning => warning.Contains("missing=[0]")), Is.True);
        }

        [Test]
        public void ValidateScope_AddsWarningForSameValueAsDefaultLocale()
        {
            var result = ValidateSampleScope(
                new Dictionary<string, string> { ["sample.save"] = "保存" },
                new Dictionary<string, string> { ["sample.save"] = "保存" });

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings.Any(warning => warning.Contains("defaultLocaleと同一の値です")), Is.True);
        }

        [Test]
        public void ValidateScope_KeepsPlaceholderMismatchAsError()
        {
            var result = ValidateSampleScope(
                new Dictionary<string, string> { ["sample.count"] = "項目 {0}" },
                new Dictionary<string, string> { ["sample.count"] = "Items {1}" });

            Assert.That(result.Errors.Any(error => error.Contains("placeholderが一致しません")), Is.True);
        }

        private static EditorL10nValidationResult ValidateSampleScope(
            Dictionary<string, string> defaultTable,
            Dictionary<string, string> englishTable)
        {
            var scope = new EditorL10nScopeCatalog("sample", "ja", "Assets/Sample/l10n-manifest.json");
            scope.AddLocale(new EditorL10nLocaleInfo("ja", "日本語", "Japanese"), defaultTable, "Assets/Sample/Locales/ja.json");
            scope.AddLocale(new EditorL10nLocaleInfo("en", "English", "English"), englishTable, "Assets/Sample/Locales/en.json");

            var result = new EditorL10nValidationResult();
            EditorL10nValidator.ValidateScope(scope, result);
            return result;
        }
    }
}
#endif
