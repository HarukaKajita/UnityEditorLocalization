#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Kajitaharuka.EditorLocalization.Tests
{
    /// <summary>
    /// グローバル設定が未設定のときのシステム（OS）言語フォールバックと、表示ロケールの由来（source）解決を検証する。
    /// システムタグの供給元は <see cref="EditorL10n.SystemLocaleProvider"/> を差し替えて環境非依存に固定する。
    /// </summary>
    [TestFixture]
    public sealed class EditorL10nSystemLocaleTests
    {
        private const string TestScope = "system-locale-test";

        private FieldInfo _catalogField;
        private Func<string> _originalProvider;
        private object _originalCatalog;
        private string _originalGlobal;
        private string _originalScopeLocale;
        private bool _originalFallbackEnabled;

        [SetUp]
        public void SetUp()
        {
            _catalogField = typeof(EditorL10n).GetField("_catalog", BindingFlags.Static | BindingFlags.NonPublic);

            // 既存設定を退避し、テストごとに既知の初期状態（scope/global 未設定・フォールバック有効）へ揃える。
            _originalProvider = EditorL10n.SystemLocaleProvider;
            _originalCatalog = _catalogField.GetValue(null);
            _originalGlobal = EditorL10nPreferences.GlobalLocale;
            _originalScopeLocale = EditorL10nPreferences.GetScopeLocale(TestScope);
            _originalFallbackEnabled = EditorL10nPreferences.SystemLocaleFallbackEnabled;

            // defaultLocale=en の scope を用意（最終フォールバック先の検証用）。
            _catalogField.SetValue(null, CreateTestCatalog());
            EditorL10nPreferences.GlobalLocale = "";
            EditorL10nPreferences.SetScopeLocale(TestScope, "");
            EditorL10nPreferences.SystemLocaleFallbackEnabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            EditorL10n.SystemLocaleProvider = _originalProvider;
            _catalogField.SetValue(null, _originalCatalog);
            EditorL10nPreferences.GlobalLocale = _originalGlobal;
            EditorL10nPreferences.SetScopeLocale(TestScope, _originalScopeLocale);
            EditorL10nPreferences.SystemLocaleFallbackEnabled = _originalFallbackEnabled;
        }

        [Test]
        public void GetSystemLocale_NormalizesProviderOutput()
        {
            EditorL10n.SystemLocaleProvider = () => "ja_JP";
            Assert.AreEqual("ja-JP", EditorL10n.GetSystemLocale());
        }

        [Test]
        public void GetSystemLocale_ReturnsEmptyForBlankProvider()
        {
            EditorL10n.SystemLocaleProvider = () => "";
            Assert.AreEqual("", EditorL10n.GetSystemLocale());
        }

        [Test]
        public void GetSystemLocale_RecomputesWhenProviderOutputChanges()
        {
            EditorL10n.SystemLocaleProvider = () => "ja_JP";
            Assert.AreEqual("ja-JP", EditorL10n.GetSystemLocale());

            // 正規化結果はメモ化されるが、生タグが変われば（供給元差し替え／OS 言語変更を模す）
            // 無効化して再計算する。陳腐化しないことを固定する。
            EditorL10n.SystemLocaleProvider = () => "fr_FR";
            Assert.AreEqual("fr-FR", EditorL10n.GetSystemLocale());
        }

        [TestCase(SystemLanguage.Japanese, "ja")]
        [TestCase(SystemLanguage.English, "en")]
        [TestCase(SystemLanguage.ChineseSimplified, "zh-Hans")]
        [TestCase(SystemLanguage.ChineseTraditional, "zh-Hant")]
        [TestCase(SystemLanguage.Portuguese, "pt")]
        [TestCase(SystemLanguage.Unknown, "")]
        public void SystemLanguageToTag_MapsKnownLanguagesAndDegradesUnknown(SystemLanguage language, string expected)
        {
            Assert.AreEqual(expected, EditorL10n.SystemLanguageToTag(language));
        }

        [Test]
        public void RefineWithRegion_AppendsRegionOnlyWhenLanguageAgrees()
        {
            // 言語が一致する CultureInfo タグなら地域を補う。
            Assert.AreEqual("ja-JP", EditorL10n.RefineWithRegion("ja", "ja-JP"));
            // 言語が食い違う（OS=日本語だが CultureInfo が en-US 等）なら地域を採らず言語タグのまま。
            Assert.AreEqual("ja", EditorL10n.RefineWithRegion("ja", "en-US"));
            // script を含むタグは地域で上書きせず保持する。
            Assert.AreEqual("zh-Hans", EditorL10n.RefineWithRegion("zh-Hans", "zh-CN"));
            // CultureInfo が空でも安全に言語タグを返す。
            Assert.AreEqual("ja", EditorL10n.RefineWithRegion("ja", ""));
        }

        [Test]
        public void GetActiveLocale_UsesSystemLocaleWhenGlobalUnsetAndEnabled()
        {
            EditorL10n.SystemLocaleProvider = () => "ja-JP";

            var active = EditorL10n.GetActiveLocale(TestScope, out var source);

            Assert.AreEqual("ja-JP", active);
            Assert.AreEqual(EditorL10nLocaleSource.System, source);
        }

        [Test]
        public void GetActiveLocale_ReturnsRawSystemTagEvenWhenUnavailableInScope()
        {
            // 解決順の責務はあくまで「要求ロケール」を返すことで、利用可能性の解決（fallback 連鎖）は
            // 上位（TryTranslate / UI）の責務。カタログ外のタグでもそのまま System として返す契約を固定する。
            EditorL10n.SystemLocaleProvider = () => "de-DE";

            var active = EditorL10n.GetActiveLocale(TestScope, out var source);

            Assert.AreEqual("de-DE", active);
            Assert.AreEqual(EditorL10nLocaleSource.System, source);
        }

        [Test]
        public void GetActiveLocale_SkipsSystemLocaleWhenFallbackDisabled()
        {
            EditorL10n.SystemLocaleProvider = () => "ja-JP";
            EditorL10nPreferences.SystemLocaleFallbackEnabled = false;

            var active = EditorL10n.GetActiveLocale(TestScope, out var source);

            Assert.AreEqual("en", active);
            Assert.AreEqual(EditorL10nLocaleSource.Default, source);
        }

        [Test]
        public void GetActiveLocale_FallsToDefaultWhenSystemLocaleUndetected()
        {
            EditorL10n.SystemLocaleProvider = () => "";

            var active = EditorL10n.GetActiveLocale(TestScope, out var source);

            Assert.AreEqual("en", active);
            Assert.AreEqual(EditorL10nLocaleSource.Default, source);
        }

        [Test]
        public void GetActiveLocale_GlobalTakesPriorityOverSystem()
        {
            EditorL10n.SystemLocaleProvider = () => "ja-JP";
            EditorL10nPreferences.GlobalLocale = "fr";

            var active = EditorL10n.GetActiveLocale(TestScope, out var source);

            Assert.AreEqual("fr", active);
            Assert.AreEqual(EditorL10nLocaleSource.Global, source);
        }

        [Test]
        public void GetActiveLocale_ScopeOverrideTakesPriorityOverSystem()
        {
            EditorL10n.SystemLocaleProvider = () => "ja-JP";
            EditorL10nPreferences.SetScopeLocale(TestScope, "de");

            var active = EditorL10n.GetActiveLocale(TestScope, out var source);

            Assert.AreEqual("de", active);
            Assert.AreEqual(EditorL10nLocaleSource.ScopeOverride, source);
        }

        private static EditorL10nCatalog CreateTestCatalog()
        {
            var catalog = new EditorL10nCatalog();
            var scopeCatalog = new EditorL10nScopeCatalog(TestScope, "en", "Assets/Test/l10n-manifest.json");
            scopeCatalog.AddLocale(
                new EditorL10nLocaleInfo("en", "English", "English"),
                new Dictionary<string, string> { ["sample.key"] = "Value" },
                "Assets/Test/Locales/en.json");
            scopeCatalog.AddLocale(
                new EditorL10nLocaleInfo("ja", "日本語", "Japanese"),
                new Dictionary<string, string> { ["sample.key"] = "値" },
                "Assets/Test/Locales/ja.json");

            var scopesField = typeof(EditorL10nCatalog).GetField("_scopes", BindingFlags.Instance | BindingFlags.NonPublic);
            var scopes = (Dictionary<string, EditorL10nScopeCatalog>)scopesField.GetValue(catalog);
            scopes.Add(TestScope, scopeCatalog);
            return catalog;
        }
    }
}
#endif
