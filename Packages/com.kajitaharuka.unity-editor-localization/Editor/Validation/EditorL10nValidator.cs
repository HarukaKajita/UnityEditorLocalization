#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>検証で見つかった問題の深刻度。</summary>
    public enum EditorL10nValidationSeverity
    {
        Error,
        Warning,
    }

    /// <summary>
    /// 検証で見つかった 1 件の問題。由来（scope / locale）と詳細メッセージを構造化して保持し、
    /// UI が scope ごとに分類して表示できるようにする。Console 用の平坦な 1 行表現も提供する。
    /// </summary>
    public sealed class EditorL10nValidationIssue
    {
        public EditorL10nValidationSeverity Severity { get; }
        public string Scope { get; }
        public string Locale { get; }
        public string Message { get; }

        internal EditorL10nValidationIssue(EditorL10nValidationSeverity severity, string scope, string locale, string message)
        {
            Severity = severity;
            Scope = scope ?? "";
            Locale = locale ?? "";
            Message = message ?? "";
        }

        /// <summary>Console など平坦な 1 行表示用（<c>{scope}/{locale}: {message}</c> 形式）。</summary>
        public string ToLogLine()
        {
            if (string.IsNullOrEmpty(Scope))
                return Message;
            var prefix = string.IsNullOrEmpty(Locale) ? Scope : Scope + "/" + Locale;
            return prefix + ": " + Message;
        }

        public override string ToString() => ToLogLine();
    }

    public sealed class EditorL10nValidationResult
    {
        private readonly List<EditorL10nValidationIssue> _issues = new();
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        /// <summary>scope ごとの分類表示に使う構造化された問題一覧（追加順）。</summary>
        public IReadOnlyList<EditorL10nValidationIssue> Issues => _issues;
        /// <summary>互換維持・Console 用の平坦なエラーメッセージ（<c>{scope}/{locale}: 詳細</c>）。</summary>
        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;

        // scope / locale / 詳細を受け取り、構造化 issue と平坦文字列の双方を蓄える（表示と Console で使い分ける）。
        internal void AddError(string scope, string locale, string detail)
        {
            if (string.IsNullOrEmpty(detail))
                return;
            var issue = new EditorL10nValidationIssue(EditorL10nValidationSeverity.Error, scope, locale, detail);
            _issues.Add(issue);
            _errors.Add(issue.ToLogLine());
        }

        internal void AddWarning(string scope, string locale, string detail)
        {
            if (string.IsNullOrEmpty(detail))
                return;
            var issue = new EditorL10nValidationIssue(EditorL10nValidationSeverity.Warning, scope, locale, detail);
            _issues.Add(issue);
            _warnings.Add(issue.ToLogLine());
        }
    }

    /// <summary>
    /// 翻訳テーブルの欠落キー、format placeholder、未翻訳の疑いを検証する。
    /// </summary>
    public static class EditorL10nValidator
    {
        private static readonly Regex PlaceholderRegex = new(@"(?<!\{)\{(\d+)(?:[^}]*)\}(?!\})", RegexOptions.Compiled);

        [MenuItem("Tools/UnityEditorLocalization/Reload Catalogs")]
        public static void ReloadCatalogs()
        {
            EditorL10n.Reload();
            Debug.Log("EditorLocalization: カタログを再読み込みしました。");
        }

        [MenuItem("Tools/UnityEditorLocalization/Validate Catalogs")]
        public static void ValidateCatalogsMenu()
        {
            ValidateAndLog();
        }

        /// <summary>
        /// 全カタログを検証し、各 warning/error と総括を Console へ出力したうえで結果を返す。
        /// メニューと Preferences の検証ボタンの双方から使う共通入口。
        /// </summary>
        public static EditorL10nValidationResult ValidateAndLog()
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

            return result;
        }

        public static EditorL10nValidationResult ValidateAll()
        {
            EditorL10n.Reload();
            var result = new EditorL10nValidationResult();
            foreach (var scope in EditorL10n.Catalog.Scopes)
                ValidateScope(scope, result);
            return result;
        }

        internal static void ValidateScope(EditorL10nScopeCatalog scope, EditorL10nValidationResult result)
        {
            if (string.IsNullOrEmpty(scope.DefaultLocale))
                result.AddError(scope.Scope, "", "defaultLocaleが空です。");
            if (!scope.HasLocale(scope.DefaultLocale))
                result.AddError(scope.Scope, "", $"defaultLocaleのテーブルがありません: {scope.DefaultLocale}");

            if (!scope.TablesByLocale.TryGetValue(scope.DefaultLocale, out var defaultTable))
                return;

            var defaultKeys = new HashSet<string>(defaultTable.Keys);
            foreach (var tablePair in scope.TablesByLocale)
            {
                var locale = tablePair.Key;
                var table = tablePair.Value;
                foreach (var missingKey in defaultKeys.Except(table.Keys).OrderBy(key => key))
                    result.AddError(scope.Scope, locale, $"keyが不足しています: {missingKey}");
                foreach (var extraKey in table.Keys.Except(defaultKeys).OrderBy(key => key))
                    result.AddWarning(scope.Scope, locale, $"defaultLocaleにないkeyがあります: {extraKey}");

                foreach (var key in table.Keys.OrderBy(key => key))
                {
                    var placeholders = ExtractPlaceholders(table[key]);
                    var missingNumbers = FindMissingPlaceholderNumbers(placeholders);
                    if (missingNumbers.Count > 0)
                    {
                        var present = FormatPlaceholders(placeholders);
                        var missing = string.Join(",", missingNumbers);
                        result.AddWarning(scope.Scope, locale, $"placeholder番号が連続していません: {key} present=[{present}] missing=[{missing}]");
                    }
                }

                foreach (var key in defaultKeys.Intersect(table.Keys).OrderBy(key => key))
                {
                    var expected = ExtractPlaceholders(defaultTable[key]);
                    var actual = ExtractPlaceholders(table[key]);
                    if (!expected.SetEquals(actual))
                        result.AddError(scope.Scope, locale, $"placeholderが一致しません: {key} expected=[{FormatPlaceholders(expected)}] actual=[{FormatPlaceholders(actual)}]");

                    if (locale != scope.DefaultLocale && table[key] == defaultTable[key])
                        result.AddWarning(scope.Scope, locale, $"defaultLocaleと同一の値です（未翻訳の可能性）: {key}");
                }
            }
        }

        internal static HashSet<string> ExtractPlaceholders(string text)
        {
            var placeholders = new HashSet<string>();
            if (string.IsNullOrEmpty(text))
                return placeholders;

            foreach (Match match in PlaceholderRegex.Matches(text))
                placeholders.Add(match.Groups[1].Value);
            return placeholders;
        }

        internal static IReadOnlyList<int> FindMissingPlaceholderNumbers(IEnumerable<string> placeholders)
        {
            var numbers = placeholders
                .Select(ParsePlaceholderNumber)
                .Where(number => number >= 0)
                .Distinct()
                .OrderBy(number => number)
                .ToArray();

            if (numbers.Length == 0)
                return Array.Empty<int>();

            var present = new HashSet<int>(numbers);
            var missing = new List<int>();
            for (var number = 0; number <= numbers[numbers.Length - 1]; number++)
            {
                if (!present.Contains(number))
                    missing.Add(number);
            }

            return missing;
        }

        private static string FormatPlaceholders(IEnumerable<string> placeholders)
        {
            return string.Join(",", placeholders
                .Select(placeholder => new { Raw = placeholder, Number = ParsePlaceholderNumber(placeholder) })
                .OrderBy(placeholder => placeholder.Number < 0 ? int.MaxValue : placeholder.Number)
                .ThenBy(placeholder => placeholder.Raw)
                .Select(placeholder => placeholder.Raw));
        }

        private static int ParsePlaceholderNumber(string placeholder)
        {
            return int.TryParse(placeholder, out var number) ? number : -1;
        }
    }
}
#endif
