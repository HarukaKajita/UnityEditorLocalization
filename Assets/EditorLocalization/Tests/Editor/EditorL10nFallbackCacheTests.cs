#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Kajitaharuka.EditorLocalization.Tests
{
    [TestFixture]
    public sealed class EditorL10nFallbackCacheTests
    {
        private const string TestScope = "fallback-cache-test";

        [Test]
        public void GetFallbackChain_ReturnsSameSequenceAsBuildFallbackChain()
        {
            AssertCachedChainMatchesBuildFallbackChain("es-419", "ja");
            AssertCachedChainMatchesBuildFallbackChain("es-419", "es");
            AssertCachedChainMatchesBuildFallbackChain("", "zh-Hant");
            AssertCachedChainMatchesBuildFallbackChain(null, "ja");
        }

        [Test]
        public void GetFallbackChain_ReusesCachedArrayForSameLocalePair()
        {
            EditorL10n.Reload();

            var first = EditorL10n.GetFallbackChain("es-419", "ja");
            var second = EditorL10n.GetFallbackChain("es-419", "ja");

            Assert.AreSame(first, second);
            CollectionAssert.AreEqual(new[] { "es-419", "es", "ja" }, second);
        }

        [Test]
        public void Reload_ClearsFallbackChainCache()
        {
            var beforeReload = EditorL10n.GetFallbackChain("zh-Hant-TW", "ja");

            EditorL10n.Reload();
            var afterReload = EditorL10n.GetFallbackChain("zh-Hant-TW", "ja");

            Assert.AreNotSame(beforeReload, afterReload);
            CollectionAssert.AreEqual(beforeReload, afterReload);
        }

        [Test]
        public void TryTranslate_KeepsFallbackResultWhenFallbackChainIsCached()
        {
            var catalogField = GetCatalogField();
            var originalCatalog = catalogField.GetValue(null);
            var originalScopeLocale = EditorL10nPreferences.GetScopeLocale(TestScope);

            try
            {
                catalogField.SetValue(null, CreateTestCatalog());
                EditorL10nPreferences.SetScopeLocale(TestScope, "es-419");

                Assert.IsTrue(EditorL10n.TryTranslate(TestScope, "sample.parent", out var firstParentText));
                Assert.AreEqual("Texto padre", firstParentText);

                Assert.IsTrue(EditorL10n.TryTranslate(TestScope, "sample.parent", out var cachedParentText));
                Assert.AreEqual("Texto padre", cachedParentText);

                Assert.IsTrue(EditorL10n.TryTranslate(TestScope, "sample.default", out var defaultText));
                Assert.AreEqual("既定値", defaultText);
            }
            finally
            {
                catalogField.SetValue(null, originalCatalog);
                EditorL10nPreferences.SetScopeLocale(TestScope, originalScopeLocale);
            }
        }

        private static void AssertCachedChainMatchesBuildFallbackChain(string locale, string defaultLocale)
        {
            CollectionAssert.AreEqual(
                EditorL10n.BuildFallbackChain(locale, defaultLocale).ToArray(),
                EditorL10n.GetFallbackChain(locale, defaultLocale));
        }

        private static EditorL10nCatalog CreateTestCatalog()
        {
            var catalog = new EditorL10nCatalog();
            var scopeCatalog = new EditorL10nScopeCatalog(TestScope, "ja", "Assets/Test/l10n-manifest.json");
            scopeCatalog.AddLocale(
                new EditorL10nLocaleInfo("ja", "日本語", "Japanese"),
                new Dictionary<string, string>
                {
                    ["sample.default"] = "既定値",
                    ["sample.parent"] = "既定の親"
                },
                "Assets/Test/Locales/ja.json");
            scopeCatalog.AddLocale(
                new EditorL10nLocaleInfo("es", "Español", "Spanish"),
                new Dictionary<string, string>
                {
                    ["sample.parent"] = "Texto padre"
                },
                "Assets/Test/Locales/es.json");

            var scopesField = typeof(EditorL10nCatalog).GetField("_scopes", BindingFlags.Instance | BindingFlags.NonPublic);
            var scopes = (Dictionary<string, EditorL10nScopeCatalog>)scopesField.GetValue(catalog);
            scopes.Add(TestScope, scopeCatalog);
            return catalog;
        }

        private static FieldInfo GetCatalogField()
        {
            return typeof(EditorL10n).GetField("_catalog", BindingFlags.Static | BindingFlags.NonPublic);
        }
    }
}
#endif
