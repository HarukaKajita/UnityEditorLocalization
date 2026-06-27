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
    /// 診断メッセージの種類。詳細文を文字列で固定せず種類＋引数で持つことで、表示時に
    /// パッケージ自身の翻訳カタログから現在の表示言語で整形できる（多言語化・言語追従）。
    /// </summary>
    public enum EditorL10nValidationMessageKind
    {
        DefaultLocaleEmpty,
        DefaultLocaleNoTable,
        MissingKey,
        ExtraKey,
        PlaceholderGap,
        PlaceholderMismatch,
        SameAsDefault,
    }

    /// <summary>
    /// 検証で見つかった 1 件の問題。由来（scope / locale）と、メッセージ種類＋整形引数を構造化して
    /// 保持し、UI が scope ごとに分類して表示できるようにする。詳細文は表示時に翻訳カタログから
    /// 整形するため、表示言語の変更に追従する。Console 用の平坦な 1 行表現も提供する。
    /// </summary>
    public sealed class EditorL10nValidationIssue
    {
        public EditorL10nValidationSeverity Severity { get; }
        public EditorL10nValidationMessageKind Kind { get; }
        public string Scope { get; }
        public string Locale { get; }
        /// <summary>メッセージ整形に渡す引数（key 名・locale タグ・placeholder 一覧などの機械的トークン。翻訳しない）。</summary>
        public IReadOnlyList<string> Args { get; }

        internal EditorL10nValidationIssue(EditorL10nValidationSeverity severity, EditorL10nValidationMessageKind kind,
            string scope, string locale, string[] args)
        {
            Severity = severity;
            Kind = kind;
            Scope = scope ?? "";
            Locale = locale ?? "";
            Args = args ?? Array.Empty<string>();
        }

        /// <summary>詳細メッセージ。表示時にパッケージ自身の翻訳カタログから現在の表示言語で整形する。</summary>
        public string Message => EditorL10nValidationMessage.Format(Kind, Args);

        /// <summary>Console など平坦な 1 行表示用（<c>{scope}/{locale}: {詳細}</c> 形式）。</summary>
        public string ToLogLine()
        {
            if (string.IsNullOrEmpty(Scope))
                return Message;
            var prefix = string.IsNullOrEmpty(Locale) ? Scope : Scope + "/" + Locale;
            return prefix + ": " + Message;
        }

        public override string ToString() => ToLogLine();
    }

    /// <summary>診断メッセージ種類を翻訳キーへ対応付け、パッケージ自身のカタログから詳細文を整形する。</summary>
    internal static class EditorL10nValidationMessage
    {
        // 診断文はこのパッケージ自身の翻訳カタログ（scope=パッケージ名）から引く。
        // 検証対象 scope の内容ではなく「検証ツールの文言」なので、パッケージ自身の scope を使うのが妥当。
        private const string UiScope = "com.kajitaharuka.unity-editor-localization";

        public static string Format(EditorL10nValidationMessageKind kind, IReadOnlyList<string> args)
        {
            // Tr は params object[] を取るため、string 引数を object[] へ移送する。
            var formatArgs = new object[args?.Count ?? 0];
            for (var i = 0; i < formatArgs.Length; i++)
                formatArgs[i] = args[i];
            return EditorL10n.Tr(UiScope, KeyFor(kind), formatArgs);
        }

        public static string KeyFor(EditorL10nValidationMessageKind kind) => kind switch
        {
            EditorL10nValidationMessageKind.DefaultLocaleEmpty => "validation.defaultLocaleEmpty",
            EditorL10nValidationMessageKind.DefaultLocaleNoTable => "validation.defaultLocaleNoTable",
            EditorL10nValidationMessageKind.MissingKey => "validation.missingKey",
            EditorL10nValidationMessageKind.ExtraKey => "validation.extraKey",
            EditorL10nValidationMessageKind.PlaceholderGap => "validation.placeholderGap",
            EditorL10nValidationMessageKind.PlaceholderMismatch => "validation.placeholderMismatch",
            _ => "validation.sameAsDefault",
        };
    }

    public sealed class EditorL10nValidationResult
    {
        private readonly List<EditorL10nValidationIssue> _issues = new();
        private int _errorCount;
        private int _warningCount;

        /// <summary>scope ごとの分類表示に使う構造化された問題一覧（追加順）。</summary>
        public IReadOnlyList<EditorL10nValidationIssue> Issues => _issues;
        public int ErrorCount => _errorCount;
        public int WarningCount => _warningCount;
        public bool IsValid => _errorCount == 0;

        /// <summary>互換維持・Console 用の平坦なエラー文（<c>{scope}/{locale}: 詳細</c>）。現在の表示言語で整形される。</summary>
        public IReadOnlyList<string> Errors => Project(EditorL10nValidationSeverity.Error);
        public IReadOnlyList<string> Warnings => Project(EditorL10nValidationSeverity.Warning);

        private List<string> Project(EditorL10nValidationSeverity severity)
        {
            var list = new List<string>();
            foreach (var issue in _issues)
                if (issue.Severity == severity)
                    list.Add(issue.ToLogLine());
            return list;
        }

        // scope / locale / メッセージ種類＋引数を受け取り、構造化 issue として蓄える（表示時に整形）。
        internal void AddError(string scope, string locale, EditorL10nValidationMessageKind kind, params string[] args)
        {
            _issues.Add(new EditorL10nValidationIssue(EditorL10nValidationSeverity.Error, kind, scope, locale, args));
            _errorCount++;
        }

        internal void AddWarning(string scope, string locale, EditorL10nValidationMessageKind kind, params string[] args)
        {
            _issues.Add(new EditorL10nValidationIssue(EditorL10nValidationSeverity.Warning, kind, scope, locale, args));
            _warningCount++;
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
                Debug.Log($"EditorLocalization: 検証に成功しました。Warnings: {result.WarningCount}");
            else
                Debug.LogError($"EditorLocalization: 検証に失敗しました。Errors: {result.ErrorCount}, Warnings: {result.WarningCount}");

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
                result.AddError(scope.Scope, "", EditorL10nValidationMessageKind.DefaultLocaleEmpty);
            if (!scope.HasLocale(scope.DefaultLocale))
                result.AddError(scope.Scope, "", EditorL10nValidationMessageKind.DefaultLocaleNoTable, scope.DefaultLocale);

            if (!scope.TablesByLocale.TryGetValue(scope.DefaultLocale, out var defaultTable))
                return;

            var defaultKeys = new HashSet<string>(defaultTable.Keys);
            foreach (var tablePair in scope.TablesByLocale)
            {
                var locale = tablePair.Key;
                var table = tablePair.Value;
                foreach (var missingKey in defaultKeys.Except(table.Keys).OrderBy(key => key))
                    result.AddError(scope.Scope, locale, EditorL10nValidationMessageKind.MissingKey, missingKey);
                foreach (var extraKey in table.Keys.Except(defaultKeys).OrderBy(key => key))
                    result.AddWarning(scope.Scope, locale, EditorL10nValidationMessageKind.ExtraKey, extraKey);

                foreach (var key in table.Keys.OrderBy(key => key))
                {
                    var placeholders = ExtractPlaceholders(table[key]);
                    var missingNumbers = FindMissingPlaceholderNumbers(placeholders);
                    if (missingNumbers.Count > 0)
                    {
                        var present = FormatPlaceholders(placeholders);
                        var missing = string.Join(",", missingNumbers);
                        result.AddWarning(scope.Scope, locale, EditorL10nValidationMessageKind.PlaceholderGap, key, present, missing);
                    }
                }

                foreach (var key in defaultKeys.Intersect(table.Keys).OrderBy(key => key))
                {
                    var expected = ExtractPlaceholders(defaultTable[key]);
                    var actual = ExtractPlaceholders(table[key]);
                    if (!expected.SetEquals(actual))
                        result.AddError(scope.Scope, locale, EditorL10nValidationMessageKind.PlaceholderMismatch,
                            key, FormatPlaceholders(expected), FormatPlaceholders(actual));

                    if (locale != scope.DefaultLocale && table[key] == defaultTable[key])
                        result.AddWarning(scope.Scope, locale, EditorL10nValidationMessageKind.SameAsDefault, key);
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
