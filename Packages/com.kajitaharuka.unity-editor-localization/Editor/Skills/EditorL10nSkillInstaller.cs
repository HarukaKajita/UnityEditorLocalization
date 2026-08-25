#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>スキル登録先 1 スコープ（ユーザー/プロジェクト）ぶんの登録状態。Preferences の状態ピル表示用。</summary>
    internal enum EditorL10nSkillInstallState
    {
        /// <summary>期待するリンクが 1 つも無い。</summary>
        NotInstalled,
        /// <summary>期待するリンクがすべて有効（実体へ辿れる）。</summary>
        Installed,
        /// <summary>リンクの一部が欠けている、またはリンク先の実体が無い（再登録で直る）。</summary>
        NeedsReinstall,
    }

    /// <summary>スキル 1 件ぶんの登録処理の結果。要約の内訳表示に使う。</summary>
    internal enum EditorL10nSkillEntryAction
    {
        /// <summary>symlink（または junction）を張り直した。</summary>
        Linked,

        /// <summary>登録先が実体ディレクトリだったので、中身を正本へ更新した。</summary>
        Copied,

        /// <summary>登録先が実体ディレクトリで、中身が既に正本と一致していた。</summary>
        AlreadyCurrent,

        /// <summary>登録に失敗した。</summary>
        Failed,
    }

    /// <summary>
    /// パッケージ同梱の AIエージェント向けスキル（翻訳ワークフロー / 既存拡張の多言語化連携）を、
    /// <c>.claude/skills</c> と <c>.agents/skills</c> へ symlink で登録するインストーラ（Claude Code 等で利用）。
    /// 登録先スコープはユーザー（ホーム）かプロジェクト（リポジトリ直下）を選べる。
    /// 同等の操作を行う CLI コマンドも生成し、CLI ユーザーがコピペで実行できるようにする。
    /// </summary>
    internal static class EditorL10nSkillInstaller
    {
        private const string PackageName = EditorL10nPackage.Name;

        // 同梱スキルのフォルダ名。skills/ を解決できないときだけ使うフォールバックで、
        // 通常は GetSkillFolders() がパッケージの skills/ を実体から列挙する。
        // 固定配列だけに頼ると、スキルを増やしたときに配列の更新漏れでそのスキルが
        // 永久に登録されない（利用者からは「導入したのに無い」としか見えない）。
        private static readonly string[] KnownSkillFolders =
        {
            "editor-localization-translation-quality",
            "editor-localization-optional-integration",
        };

        // リンク先の 2 系統。Claude Code は .claude/skills を、汎用エージェント系は .agents/skills を読む。
        private static readonly string[] LinkRoots = { ".claude/skills", ".agents/skills" };

        /// <summary>
        /// 登録対象のスキルフォルダ名を返します。パッケージの <c>skills/</c> を実体から列挙します。
        /// </summary>
        /// <remarks>
        /// 解決できないときだけ既知の一覧へ落とします。スキルの増減へ自動で追従させることで、
        /// 一覧の更新漏れによる「増やしたスキルが登録されない」を構造的に防ぎます。
        /// </remarks>
        internal static IReadOnlyList<string> GetSkillFolders()
        {
            var root = TryGetSkillsRoot(out _);
            if (root == null)
                return KnownSkillFolders;

            var names = new List<string>();
            foreach (var dir in Directory.GetDirectories(root))
            {
                names.Add(Path.GetFileName(dir));
            }

            names.Sort(StringComparer.Ordinal);
            return names.Count > 0 ? (IReadOnlyList<string>)names : KnownSkillFolders;
        }

        // ===== メニュー（間接方式のみ）=====

        // GOLD_STANDARD §2.6 の間接方式: メニューは単一項目とし、押すとスキル導入の説明・登録状態・
        // 実行ボタンを持つ Preferences ペインを「開くだけ」にする。メニュー項目から直接スキル登録・
        // クリップボード書き込み等の副作用は一切行わない（意図しないスキル追加は開発者に敬遠されるため、
        // 登録はユーザーがペイン内で内容を理解したうえで明示的にボタンを押した場合にのみ行う）。
        // 実際の登録/CLI コピー処理は下記の公開ロジックとして残し、ペイン側のボタンから呼ぶ。
        [MenuItem("Tools/UnityEditorLocalization/AI Agent Skills", priority = 200)]
        private static void MenuOpenSkillsPane() =>
            SettingsService.OpenUserPreferences(EditorL10nSettingsProvider.SettingsPath);

        // ===== Preferences / ペインから呼ぶ公開ロジック =====

        /// <summary>ユーザースコープ（ホーム配下）へスキルを登録する。結果サマリ文字列を返す。</summary>
        internal static string InstallToUser() => Install(GetUserBase(), "user");

        /// <summary>プロジェクトスコープ（リポジトリ直下）へスキルを登録する。結果サマリ文字列を返す。</summary>
        internal static string InstallToProject() => Install(GetProjectBase(), "project");

        /// <summary>ユーザースコープへ登録する CLI コマンド片を返す（コピペ実行可能）。</summary>
        internal static string CliSnippetForUser()
        {
            var skillsRoot = TryGetSkillsRoot(out var error);
            if (skillsRoot == null)
                return "# " + error;
            return Application.platform == RuntimePlatform.WindowsEditor
                ? BuildWindowsSnippet("user scope (.claude, .agents)", skillsRoot, "%USERPROFILE%")
                : BuildUnixSnippet("user scope (~/.claude, ~/.agents)", skillsRoot, "~");
        }

        /// <summary>ユーザースコープ（ホーム配下）の登録状態を確認する。</summary>
        internal static EditorL10nSkillInstallState GetUserInstallState() => GetInstallState(GetUserBase());

        /// <summary>プロジェクトスコープ（リポジトリ直下）の登録状態を確認する。</summary>
        internal static EditorL10nSkillInstallState GetProjectInstallState() => GetInstallState(GetProjectBase());

        // 登録先ベースディレクトリ配下の期待リンクを走査し、登録状態へ集約する。
        // 「有効なリンク」= Directory.Exists（symlink を辿って実体まで届く）。リンク切れ（symlink は
        // あるが実体が無い）は親ディレクトリのエントリ列挙で検出する（.NET Standard 2.1 には
        // リンク解決 API が無いため）。有効/欠落/リンク切れの混在は一律「再登録が必要」に倒す。
        private static EditorL10nSkillInstallState GetInstallState(string baseDir)
        {
            if (string.IsNullOrEmpty(baseDir))
                return EditorL10nSkillInstallState.NotInstalled;

            var skillsRoot = TryGetSkillsRoot(out _);
            var valid = 0;
            var brokenOrMissing = 0;
            var anyEntry = false;
            foreach (var root in LinkRoots)
            {
                var linkDir = Path.Combine(baseDir, root.Replace('/', Path.DirectorySeparatorChar));
                foreach (var skill in GetSkillFolders())
                {
                    var link = Path.Combine(linkDir, skill);
                    if (Directory.Exists(link))
                    {
                        // symlink は常に正本を指すので中身の検査は要らない。実体コピーは古くなりうるため
                        // 正本と突き合わせる。ここを Directory.Exists だけで済ませると、古い実体コピーが
                        // 「登録済み」に見えて、更新が必要なことに気づけない。
                        var target = skillsRoot != null ? Path.Combine(skillsRoot, skill) : null;
                        if (IsReparsePoint(link) || target == null || DirectoriesMatch(target, link))
                        {
                            valid++;
                        }
                        else
                        {
                            brokenOrMissing++;
                        }

                        anyEntry = true;
                    }
                    else if (EntryExists(link))
                    {
                        // エントリはあるのに辿れない＝リンク切れ。
                        brokenOrMissing++;
                        anyEntry = true;
                    }
                    else
                    {
                        brokenOrMissing++;
                    }
                }
            }

            if (!anyEntry)
                return EditorL10nSkillInstallState.NotInstalled;
            return brokenOrMissing == 0
                ? EditorL10nSkillInstallState.Installed
                : EditorL10nSkillInstallState.NeedsReinstall;
        }

        // Directory/File.Exists は symlink を辿るため、リンク切れ symlink の存在は親のエントリ列挙で確認する。
        private static bool EntryExists(string path)
        {
            try
            {
                var parent = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                    return false;
                var name = Path.GetFileName(path);
                foreach (var entry in Directory.EnumerateFileSystemEntries(parent))
                {
                    if (string.Equals(Path.GetFileName(entry), name, StringComparison.Ordinal))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>プロジェクトスコープへ登録する CLI コマンド片を返す（コピペ実行可能）。</summary>
        internal static string CliSnippetForProject()
        {
            var skillsRoot = TryGetSkillsRoot(out var error);
            if (skillsRoot == null)
                return "# " + error;
            return Application.platform == RuntimePlatform.WindowsEditor
                ? BuildWindowsSnippet("project scope (.claude, .agents)", skillsRoot, GetProjectBase())
                : BuildUnixSnippet("project scope (.claude, .agents)", skillsRoot, Quote(GetProjectBase()));
        }

        // ===== 実装 =====

        private static string Install(string baseDir, string scopeLabel)
        {
            var skillsRoot = TryGetSkillsRoot(out var error);
            if (skillsRoot == null)
                return "[UnityEditorLocalization] スキル登録に失敗: " + error;

            var linked = 0;
            var updated = 0;
            var unchanged = 0;
            var failed = 0;
            var log = new StringBuilder();
            foreach (var root in LinkRoots)
            {
                var linkDir = Path.Combine(baseDir, root.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(linkDir);
                foreach (var skill in GetSkillFolders())
                {
                    var target = Path.Combine(skillsRoot, skill);
                    var link = Path.Combine(linkDir, skill);
                    switch (EnsureSkillEntry(link, target, out var message))
                    {
                        case EditorL10nSkillEntryAction.Linked:
                            linked++;
                            log.AppendLine("  LINK   " + link + "  ->  " + target);
                            break;
                        case EditorL10nSkillEntryAction.Copied:
                            updated++;
                            log.AppendLine("  UPDATE " + link + "（実体コピーを正本の内容へ更新）");
                            break;
                        case EditorL10nSkillEntryAction.AlreadyCurrent:
                            unchanged++;
                            log.AppendLine("  SAME   " + link + "（実体コピーが正本と一致）");
                            break;
                        default:
                            failed++;
                            log.AppendLine("  FAIL   " + link + "  : " + message);
                            break;
                    }
                }
            }

            AppendPackageCacheWarning(log, skillsRoot);

            return $"[UnityEditorLocalization] AIエージェント連携スキルを {scopeLabel} スコープへ登録: リンク {linked} / 実体更新 {updated} / 変更なし {unchanged} / 失敗 {failed}\n{log}";
        }

        // パッケージの skills/ 実体パスを解決する。埋め込み/PackageCache のどちらでも resolvedPath で正しく解決できる。
        private static string TryGetSkillsRoot(out string error)
        {
            error = "";
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + PackageName + "/package.json");
            var root = info != null
                ? Path.Combine(info.resolvedPath, "skills")
                : Path.GetFullPath("Packages/" + PackageName + "/skills"); // 埋め込み時のフォールバック
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                error = "パッケージの skills フォルダが見つかりません: " + root;
                return null;
            }
            return root;
        }

        /// <summary>
        /// 登録先 1 件を、正本を参照する状態へ揃えます。
        /// </summary>
        /// <param name="link">登録先のパス（<c>.claude/skills/&lt;skill&gt;</c> など）。</param>
        /// <param name="target">パッケージ側の正本フォルダ。</param>
        /// <param name="message">失敗したときの理由。</param>
        /// <remarks>
        /// 登録先が実体ディレクトリのときは symlink へ置き換えず、中身を正本へ揃え直します。
        /// 開発リポジトリでは sync スクリプトがコミット済みの実体ミラーを生成しており、
        /// symlink を張るとミラーを壊すためです。ただし「実体があるから成功」で済ませてはいけません。
        /// 古い内容が残ったまま更新済みに見え、直したつもりのスキルを古いまま使い続けることになります。
        /// </remarks>
        internal static EditorL10nSkillEntryAction EnsureSkillEntry(string link, string target, out string message)
        {
            message = "";
            try
            {
                if (Directory.Exists(link) && !IsReparsePoint(link))
                {
                    if (DirectoriesMatch(target, link))
                        return EditorL10nSkillEntryAction.AlreadyCurrent;

                    RefreshCopy(target, link);
                    return EditorL10nSkillEntryAction.Copied;
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                return EditorL10nSkillEntryAction.Failed;
            }

            return CreateDirectorySymlink(link, target, out message)
                ? EditorL10nSkillEntryAction.Linked
                : EditorL10nSkillEntryAction.Failed;
        }

        /// <summary>
        /// 正本フォルダと登録先の実体コピーの中身が一致しているかを返します。
        /// </summary>
        /// <param name="sourceDir">正本フォルダ。</param>
        /// <param name="destDir">登録先の実体コピー。</param>
        /// <remarks>
        /// <c>*.meta</c> は正本側から除外して比較します（ミラーはエージェント用で Unity へ取り込まれないため）。
        /// 登録先の余分なファイルは不一致として扱い、<see cref="RefreshCopy"/> が削除します。
        /// リポジトリの sync スクリプト（scripts/sync-agent-skills.mjs）と同じ規則です。
        /// </remarks>
        internal static bool DirectoriesMatch(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir) || !Directory.Exists(destDir))
                return false;

            var sourceFiles = ListRelativeFiles(sourceDir, true);
            var destFiles = ListRelativeFiles(destDir, false);
            if (sourceFiles.Count != destFiles.Count)
                return false;

            foreach (var relative in sourceFiles)
            {
                if (!destFiles.Contains(relative))
                    return false;
                if (!FilesEqual(Path.Combine(sourceDir, relative), Path.Combine(destDir, relative)))
                    return false;
            }

            return true;
        }

        // 正本の内容へ実体コピーを揃え直す。余分なファイルは消し、空になったフォルダも片付ける。
        private static void RefreshCopy(string sourceDir, string destDir)
        {
            var sourceFiles = ListRelativeFiles(sourceDir, true);
            foreach (var relative in ListRelativeFiles(destDir, false))
            {
                if (!sourceFiles.Contains(relative))
                    File.Delete(Path.Combine(destDir, relative));
            }

            foreach (var relative in sourceFiles)
            {
                var destination = Path.Combine(destDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(Path.Combine(sourceDir, relative), destination, true);
            }

            PruneEmptyDirectories(destDir);
        }

        // 配下のファイルを、区切りを '/' へ正規化した相対パスの集合で返す。
        private static SortedSet<string> ListRelativeFiles(string root, bool excludeMeta)
        {
            var results = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (excludeMeta && file.EndsWith(".meta", StringComparison.Ordinal))
                    continue;
                results.Add(NormalizeRelative(root, file));
            }

            return results;
        }

        private static string NormalizeRelative(string root, string path) =>
            path.Substring(root.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');

        private static bool FilesEqual(string a, string b)
        {
            var infoA = new FileInfo(a);
            var infoB = new FileInfo(b);
            if (!infoA.Exists || !infoB.Exists || infoA.Length != infoB.Length)
                return false;

            var bytesA = File.ReadAllBytes(a);
            var bytesB = File.ReadAllBytes(b);
            for (var i = 0; i < bytesA.Length; i++)
            {
                if (bytesA[i] != bytesB[i])
                    return false;
            }

            return true;
        }

        private static void PruneEmptyDirectories(string root)
        {
            foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                if (Directory.Exists(dir) && Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length == 0)
                    Directory.Delete(dir, true);
            }
        }

        // 参照元が PackageCache だと、リンク越しの編集はパッケージの再解決で消える。
        // スキル自体を直したい開発者が気づけるよう、登録結果の要約で明示する。
        private static void AppendPackageCacheWarning(StringBuilder log, string skillsRoot)
        {
            if (skillsRoot == null || !NormalizeSeparators(skillsRoot).Contains("/Library/PackageCache/"))
                return;

            log.AppendLine("  注意: 参照元が PackageCache（Unity が再生成する一時領域）です:");
            log.AppendLine("        " + skillsRoot);
            log.AppendLine("        リンク越しにスキルを編集しても、パッケージの再解決で失われます。");
            log.AppendLine("        スキルを直すときは、リポジトリの Packages/<pkg>/skills/ を編集してください。");
        }

        internal static string NormalizeSeparators(string path) =>
            (path ?? "").Replace('\\', '/');

        // ディレクトリ symlink を作る。macOS / Linux / Windows のいずれでも動作する。
        private static bool CreateDirectorySymlink(string link, string target, out string message)
        {
            message = "";
            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    // cmd の mklink は区切りが '\' 前提。Unity のパスは '/' のことがあるため正規化する。
                    var winLink = WinPath(link);
                    var winTarget = WinPath(target);

                    // 既存の symlink/junction のみ除去を試み、実ディレクトリは触らない。
                    if (Directory.Exists(winLink) && IsReparsePoint(winLink))
                    {
                        try { Directory.Delete(winLink); } catch { /* 失敗時は mklink 側のエラーで報告 */ }
                    }

                    // まず実 symlink（/D）。管理者権限/開発者モードが無く失敗したら、昇格不要の
                    // junction（/J）で再試行する。junction でもエージェントからの読み取りは同じく機能する。
                    if (RunProcess("cmd.exe", "/c mklink /D " + Quote(winLink) + " " + Quote(winTarget), out message))
                        return true;
                    return RunProcess("cmd.exe", "/c mklink /J " + Quote(winLink) + " " + Quote(winTarget), out message);
                }

                // macOS / Linux: -s symlink, -f 既存を置換, -n 既存 symlink を辿らない（idempotent）。
                return RunProcess(UnixLnPath(), "-sfn " + Quote(target) + " " + Quote(link), out message);
            }
            catch (Exception e)
            {
                message = e.Message;
                return false;
            }
        }

        // ln の場所はディストリにより /bin/ln か /usr/bin/ln。存在する方を使う（macOS は /bin/ln）。
        private static string UnixLnPath() => File.Exists("/bin/ln") ? "/bin/ln" : "/usr/bin/ln";

        // Windows の cmd 用にパス区切りを '\' へ正規化する。
        private static string WinPath(string path) => (path ?? "").Replace('/', '\\');

        private static bool IsReparsePoint(string path)
        {
            try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
            catch { return false; }
        }

        private static bool RunProcess(string fileName, string arguments, out string output)
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var process = Process.Start(psi))
            {
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                output = (stdout + stderr).Trim();
                return process.ExitCode == 0;
            }
        }

        // macOS/Linux 用のコピペ可能なコマンド片を組む。baseExpr は ~ または "/abs/project" のように渡す。
        private static string BuildUnixSnippet(string title, string skillsRoot, string baseExpr)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# UnityEditorLocalization skills — " + title);
            sb.AppendLine("# 注意: 登録先に実体ディレクトリがある場合、ln -sfn は置き換えません。");
            sb.AppendLine("#       内容を更新するには Preferences のボタンから登録し直してください。");
            sb.AppendLine("PKG=" + Quote(skillsRoot));
            sb.AppendLine("for s in " + string.Join(" ", GetSkillFolders()) + "; do");
            sb.AppendLine("  mkdir -p " + baseExpr + "/.claude/skills " + baseExpr + "/.agents/skills");
            sb.AppendLine("  ln -sfn \"$PKG/$s\" " + baseExpr + "/.claude/skills/\"$s\"");
            sb.AppendLine("  ln -sfn \"$PKG/$s\" " + baseExpr + "/.agents/skills/\"$s\"");
            sb.AppendLine("done");
            return sb.ToString();
        }

        // Windows（cmd.exe）用のコピペ可能なコマンド片を組む。baseExpr は %USERPROFILE% または絶対パス。
        // 区切りは '\' に正規化する。mklink /D が権限で失敗する場合は /D を /J（junction）に読み替える。
        private static string BuildWindowsSnippet(string title, string skillsRoot, string baseExpr)
        {
            var pkg = WinPath(skillsRoot);
            var bse = WinPath(baseExpr);
            var sb = new StringBuilder();
            sb.AppendLine(":: UnityEditorLocalization skills — " + title);
            sb.AppendLine(":: cmd.exe で実行（mklink /D が権限で失敗する場合は /D を /J に変えてください）");
            sb.AppendLine(":: 注意: 登録先に実体ディレクトリがある場合、mklink は失敗します。");
            sb.AppendLine("::       内容を更新するには Preferences のボタンから登録し直してください。");
            sb.AppendLine("set \"PKG=" + pkg + "\"");
            sb.AppendLine("mkdir \"" + bse + "\\.claude\\skills\" 2>nul");
            sb.AppendLine("mkdir \"" + bse + "\\.agents\\skills\" 2>nul");
            foreach (var s in GetSkillFolders())
            {
                sb.AppendLine("mklink /D \"" + bse + "\\.claude\\skills\\" + s + "\" \"%PKG%\\" + s + "\"");
                sb.AppendLine("mklink /D \"" + bse + "\\.agents\\skills\\" + s + "\" \"%PKG%\\" + s + "\"");
            }
            return sb.ToString();
        }

        private static string GetUserBase()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
                home = Environment.GetEnvironmentVariable("HOME") ?? "";
            return home;
        }

        // プロジェクトルート（Assets の 1 つ上）。
        private static string GetProjectBase() => Path.GetDirectoryName(Application.dataPath);

        private static string Quote(string path) => "\"" + path + "\"";
    }
}
#endif
