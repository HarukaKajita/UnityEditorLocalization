#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Kajitaharuka.EditorLocalization
{
    public sealed class EditorL10nValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;

        internal void AddError(string message)
        {
            if (!string.IsNullOrEmpty(message))
                _errors.Add(message);
        }

        internal void AddWarning(string message)
        {
            if (!string.IsNullOrEmpty(message))
                _warnings.Add(message);
        }
    }

    /// <summary>
    /// 翻訳テーブルの欠落キーとformat placeholderの不一致を検証する。
    /// </summary>
    public static class EditorL10nValidator
    {
        private static readonly Regex PlaceholderRegex = new(@"(?<!\{)\{(\d+)(?:[^}]*)\}(?!\})", RegexOptions.Compiled);

        [MenuItem("Tools/Editor Localization/Reload Catalogs")]
        public static void ReloadCatalogs()
        {
            EditorL10n.Reload();
            Debug.Log("EditorLocalization: カタログを再読み込みしました。");
        }

        [MenuItem("Tools/Editor Localization/Validate Catalogs")]
        public static void ValidateCatalogsMenu()
        {
            var result = ValidateAll();
            foreach (var warning in result.Warnings)
                Debug.LogWarning(warning);
            foreach (var error in result.Errors)
                Debug.LogError(error);

            if (result.IsValid)
                Debug.Log($"EditorLocalization: 検証に成功しました。Warnings: {result.Warnings.Count}");
            else
                Debug.LogError($"EditorLocalization: 検証に失敗しました。Errors: {result.Errors.Count}, Warnings: {result.Warnings.Count}");
        }

        public static EditorL10nValidationResult ValidateAll()
        {
            EditorL10n.Reload();
            var result = new EditorL10nValidationResult();
            foreach (var scope in EditorL10n.Catalog.Scopes)
                ValidateScope(scope, result);
            return result;
        }

        private static void ValidateScope(EditorL10nScopeCatalog scope, EditorL10nValidationResult result)
        {
            if (string.IsNullOrEmpty(scope.DefaultLocale))
                result.AddError($"{scope.Scope}: defaultLocaleが空です。");
            if (!scope.HasLocale(scope.DefaultLocale))
                result.AddError($"{scope.Scope}: defaultLocaleのテーブルがありません: {scope.DefaultLocale}");

            if (!scope.TablesByLocale.TryGetValue(scope.DefaultLocale, out var defaultTable))
                return;

            var defaultKeys = new HashSet<string>(defaultTable.Keys);
            foreach (var tablePair in scope.TablesByLocale)
            {
                var locale = tablePair.Key;
                var table = tablePair.Value;
                foreach (var missingKey in defaultKeys.Except(table.Keys).OrderBy(key => key))
                    result.AddError($"{scope.Scope}/{locale}: keyが不足しています: {missingKey}");
                foreach (var extraKey in table.Keys.Except(defaultKeys).OrderBy(key => key))
                    result.AddWarning($"{scope.Scope}/{locale}: defaultLocaleにないkeyがあります: {extraKey}");

                foreach (var key in defaultKeys.Intersect(table.Keys))
                {
                    var expected = ExtractPlaceholders(defaultTable[key]);
                    var actual = ExtractPlaceholders(table[key]);
                    if (!expected.SetEquals(actual))
                        result.AddError($"{scope.Scope}/{locale}: placeholderが一致しません: {key} expected=[{string.Join(",", expected)}] actual=[{string.Join(",", actual)}]");
                }
            }
        }

        private static HashSet<string> ExtractPlaceholders(string text)
        {
            var placeholders = new HashSet<string>();
            if (string.IsNullOrEmpty(text))
                return placeholders;

            foreach (Match match in PlaceholderRegex.Matches(text))
                placeholders.Add(match.Groups[1].Value);
            return placeholders;
        }
    }
}
#endif
