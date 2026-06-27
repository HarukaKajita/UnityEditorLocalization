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

            // 詳細文は表示言語で整形されるため、言語非依存な構造（Kind / Args）で検証する。
            // 既定表・en 表の双方が {0} を欠くため PlaceholderGap は複数件あり得る（Any で確認）。
            Assert.That(result.ErrorCount, Is.Zero);
            Assert.That(result.Issues.Any(issue =>
                    issue.Kind == EditorL10nValidationMessageKind.PlaceholderGap
                    && issue.Severity == EditorL10nValidationSeverity.Warning
                    && issue.Args.Contains("0")), // 欠落している placeholder 番号 0
                Is.True);
        }

        [Test]
        public void ValidateScope_AddsWarningForSameValueAsDefaultLocale()
        {
            var result = ValidateSampleScope(
                new Dictionary<string, string> { ["sample.save"] = "保存" },
                new Dictionary<string, string> { ["sample.save"] = "保存" });

            Assert.That(result.ErrorCount, Is.Zero);
            Assert.That(result.Issues.Any(issue =>
                    issue.Kind == EditorL10nValidationMessageKind.SameAsDefault
                    && issue.Severity == EditorL10nValidationSeverity.Warning),
                Is.True);
        }

        [Test]
        public void ValidateScope_KeepsPlaceholderMismatchAsError()
        {
            var result = ValidateSampleScope(
                new Dictionary<string, string> { ["sample.count"] = "項目 {0}" },
                new Dictionary<string, string> { ["sample.count"] = "Items {1}" });

            Assert.That(result.Issues.Any(issue =>
                    issue.Kind == EditorL10nValidationMessageKind.PlaceholderMismatch
                    && issue.Severity == EditorL10nValidationSeverity.Error),
                Is.True);
        }

        [Test]
        public void ComputeExitCode_FailsOnErrorsAndOptionallyWarnings()
        {
            // エラーがあれば常に失敗（1）。
            Assert.That(EditorL10nValidator.ComputeExitCode(1, 0, false), Is.EqualTo(1));
            Assert.That(EditorL10nValidator.ComputeExitCode(2, 3, false), Is.EqualTo(1));
            // 警告のみ: 既定は通す（0）、failOnWarnings のときだけ失敗（1）。
            Assert.That(EditorL10nValidator.ComputeExitCode(0, 3, false), Is.EqualTo(0));
            Assert.That(EditorL10nValidator.ComputeExitCode(0, 3, true), Is.EqualTo(1));
            // 問題なし: 常に 0。
            Assert.That(EditorL10nValidator.ComputeExitCode(0, 0, false), Is.EqualTo(0));
            Assert.That(EditorL10nValidator.ComputeExitCode(0, 0, true), Is.EqualTo(0));
        }

        [Test]
        public void ScopeCatalog_TryGetTablePath_ReturnsRegisteredLocalePath()
        {
            var scope = new EditorL10nScopeCatalog("sample", "ja", "Assets/Sample/l10n-manifest.json");
            scope.AddLocale(new EditorL10nLocaleInfo("ja", "日本語", "Japanese"),
                new Dictionary<string, string> { ["k"] = "v" }, "Assets/Sample/Locales/ja.json");

            Assert.That(scope.TryGetTablePath("ja", out var path), Is.True);
            Assert.That(path, Is.EqualTo("Assets/Sample/Locales/ja.json"));
            Assert.That(scope.TryGetTablePath("en", out _), Is.False);
        }

        [Test]
        public void TryGetLocaleTablePath_NormalizesLocaleTag()
        {
            // 公開 API は入力タグを正規化してから解決する（パッケージ自身のカタログで契約を固定）。
            const string scope = "com.kajitaharuka.unity-editor-localization";
            Assert.That(EditorL10n.TryGetLocaleTablePath(scope, "JA", out var path), Is.True);
            Assert.That(path, Does.EndWith("ja.json"));
        }

        [Test]
        public void CatalogWriter_WriteTable_RoundTripsWithEscaping()
        {
            var entries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a.key", "plain"),
                new KeyValuePair<string, string>("b.quote", "He said \"hi\"\nline2\tx"),
            };

            var json = EditorL10nCatalogWriter.WriteTable("ja", entries);
            var doc = UnityEngine.JsonUtility.FromJson<EditorL10nTableDocument>(json);

            Assert.That(doc.locale, Is.EqualTo("ja"));
            Assert.That(doc.entries.Length, Is.EqualTo(2));
            Assert.That(doc.entries[1].key, Is.EqualTo("b.quote"));
            Assert.That(doc.entries[1].value, Is.EqualTo("He said \"hi\"\nline2\tx"));
            // compact フォーマット（1 エントリ 1 行）を維持していること。
            Assert.That(json, Does.Contain("        { \"key\": \"a.key\", \"value\": \"plain\" },"));
        }

        [Test]
        public void CatalogWriter_WriteManifest_RoundTrips()
        {
            var document = new EditorL10nManifestDocument
            {
                scope = "com.example.tool",
                defaultLocale = "ja",
                fixedTerms = new[] { "x.fixed" },
                locales = new[]
                {
                    new EditorL10nManifestLocale { tag = "ja", nativeName = "日本語", englishName = "Japanese", tablePath = "Locales/ja.json" },
                    new EditorL10nManifestLocale { tag = "en", nativeName = "English", englishName = "English", tablePath = "Locales/en.json" },
                },
            };

            var json = EditorL10nCatalogWriter.WriteManifest(document);
            var back = UnityEngine.JsonUtility.FromJson<EditorL10nManifestDocument>(json);

            Assert.That(back.scope, Is.EqualTo("com.example.tool"));
            Assert.That(back.defaultLocale, Is.EqualTo("ja"));
            Assert.That(back.fixedTerms, Is.EqualTo(new[] { "x.fixed" }));
            Assert.That(back.locales.Length, Is.EqualTo(2));
            Assert.That(back.locales[1].tag, Is.EqualTo("en"));
            Assert.That(back.locales[1].tablePath, Is.EqualTo("Locales/en.json"));
        }

        [Test]
        public void ValidateScope_SkipsSameValueWarningForFixedTerm()
        {
            // manifest の fixedTerms に宣言した key は、全ロケールで defaultLocale と同値でも未翻訳警告を出さない。
            var result = ValidateSampleScope(
                new Dictionary<string, string> { ["sample.fixed"] = "package.json" },
                new Dictionary<string, string> { ["sample.fixed"] = "package.json" },
                fixedTerms: new[] { "sample.fixed" });

            Assert.That(result.Issues.Any(issue => issue.Kind == EditorL10nValidationMessageKind.SameAsDefault), Is.False);
        }

        private static EditorL10nValidationResult ValidateSampleScope(
            Dictionary<string, string> defaultTable,
            Dictionary<string, string> englishTable,
            string[] fixedTerms = null)
        {
            var scope = new EditorL10nScopeCatalog("sample", "ja", "Assets/Sample/l10n-manifest.json", fixedTerms);
            scope.AddLocale(new EditorL10nLocaleInfo("ja", "日本語", "Japanese"), defaultTable, "Assets/Sample/Locales/ja.json");
            scope.AddLocale(new EditorL10nLocaleInfo("en", "English", "English"), englishTable, "Assets/Sample/Locales/en.json");

            var result = new EditorL10nValidationResult();
            EditorL10nValidator.ValidateScope(scope, result);
            return result;
        }
    }
}
#endif
