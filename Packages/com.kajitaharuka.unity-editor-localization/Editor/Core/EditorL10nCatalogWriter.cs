#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// manifest / 翻訳テーブルの JSON を、パッケージ内の既存ファイルと同じ正準フォーマットで書き出す。
    /// カタログ作成ウィザードと、検証結果からの不足キー追加（クイックfix）が共用する唯一の書き出し経路。
    /// JsonUtility は entries の整形が異なるため使わず、エスケープを自前で行って整形を制御する。
    /// </summary>
    internal static class EditorL10nCatalogWriter
    {
        /// <summary>翻訳テーブル JSON（compact: 1 エントリ 1 行、4/8 スペースインデント）を組み立てる。</summary>
        internal static string WriteTable(string locale, IReadOnlyList<KeyValuePair<string, string>> entries)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("    \"locale\": \"").Append(EscapeJson(locale)).Append("\",\n");
            if (entries == null || entries.Count == 0)
            {
                sb.Append("    \"entries\": []\n");
            }
            else
            {
                sb.Append("    \"entries\": [\n");
                for (var i = 0; i < entries.Count; i++)
                {
                    sb.Append("        { \"key\": \"").Append(EscapeJson(entries[i].Key))
                        .Append("\", \"value\": \"").Append(EscapeJson(entries[i].Value)).Append("\" }");
                    sb.Append(i < entries.Count - 1 ? ",\n" : "\n");
                }

                sb.Append("    ]\n");
            }

            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>manifest JSON（2 スペースインデント、locales は複数行オブジェクト）を組み立てる。</summary>
        internal static string WriteManifest(EditorL10nManifestDocument document)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"scope\": \"").Append(EscapeJson(document.scope)).Append("\",\n");
            sb.Append("  \"defaultLocale\": \"").Append(EscapeJson(document.defaultLocale)).Append("\",\n");

            // fixedTerms（任意。空でも存在を示すため [] を出す）。
            var fixedTerms = document.fixedTerms ?? Array.Empty<string>();
            if (fixedTerms.Length == 0)
            {
                sb.Append("  \"fixedTerms\": [],\n");
            }
            else
            {
                sb.Append("  \"fixedTerms\": [\n");
                for (var i = 0; i < fixedTerms.Length; i++)
                    sb.Append("    \"").Append(EscapeJson(fixedTerms[i])).Append(i < fixedTerms.Length - 1 ? "\",\n" : "\"\n");
                sb.Append("  ],\n");
            }

            var locales = document.locales ?? Array.Empty<EditorL10nManifestLocale>();
            if (locales.Length == 0)
            {
                sb.Append("  \"locales\": []\n");
            }
            else
            {
                sb.Append("  \"locales\": [\n");
                for (var i = 0; i < locales.Length; i++)
                {
                    var locale = locales[i];
                    sb.Append("    {\n");
                    sb.Append("      \"tag\": \"").Append(EscapeJson(locale.tag)).Append("\",\n");
                    sb.Append("      \"nativeName\": \"").Append(EscapeJson(locale.nativeName)).Append("\",\n");
                    sb.Append("      \"englishName\": \"").Append(EscapeJson(locale.englishName)).Append("\",\n");
                    sb.Append("      \"tablePath\": \"").Append(EscapeJson(locale.tablePath)).Append("\"\n");
                    sb.Append(i < locales.Length - 1 ? "    },\n" : "    }\n");
                }

                sb.Append("  ]\n");
            }

            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>JSON 文字列値のエスケープ（最小実装。制御文字は \uXXXX）。JsonUtility で読み戻せる形にする。</summary>
        internal static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var sb = new StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                        {
                            // 有効なサロゲートペア（絵文字など）はそのまま出す（UTF-8 で正しく書ける）。
                            sb.Append(c).Append(value[i + 1]);
                            i++;
                        }
                        else if (char.IsSurrogate(c))
                        {
                            // 対になっていないサロゲートは \uXXXX でエスケープし、不正な UTF-8/JSON を防ぐ。
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.ToString();
        }
    }
}
#endif
