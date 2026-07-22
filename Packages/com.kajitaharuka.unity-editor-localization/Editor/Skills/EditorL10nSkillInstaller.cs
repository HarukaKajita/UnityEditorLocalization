#if UNITY_EDITOR
using System;
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

    /// <summary>
    /// パッケージ同梱の AIエージェント向けスキル（翻訳ワークフロー / 既存拡張の多言語化連携）を、
    /// <c>.claude/skills</c> と <c>.agents/skills</c> へ symlink で登録するインストーラ（Claude Code 等で利用）。
    /// 登録先スコープはユーザー（ホーム）かプロジェクト（リポジトリ直下）を選べる。
    /// 同等の操作を行う CLI コマンドも生成し、CLI ユーザーがコピペで実行できるようにする。
    /// </summary>
    internal static class EditorL10nSkillInstaller
    {
        private const string PackageName = EditorL10nPackage.Name;

        // 同梱スキルのフォルダ名（パッケージの skills/ 直下）。スキルを増やしたらここへ追加する。
        internal static readonly string[] SkillFolders =
        {
            "editor-localization-translation-quality",
            "editor-localization-optional-integration",
        };

        // リンク先の 2 系統。Claude Code は .claude/skills を、汎用エージェント系は .agents/skills を読む。
        private static readonly string[] LinkRoots = { ".claude/skills", ".agents/skills" };

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

            var valid = 0;
            var brokenOrMissing = 0;
            var anyEntry = false;
            foreach (var root in LinkRoots)
            {
                var linkDir = Path.Combine(baseDir, root.Replace('/', Path.DirectorySeparatorChar));
                foreach (var skill in SkillFolders)
                {
                    var link = Path.Combine(linkDir, skill);
                    if (Directory.Exists(link))
                    {
                        valid++;
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
            var failed = 0;
            var log = new StringBuilder();
            foreach (var root in LinkRoots)
            {
                var linkDir = Path.Combine(baseDir, root.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(linkDir);
                foreach (var skill in SkillFolders)
                {
                    var target = Path.Combine(skillsRoot, skill);
                    var link = Path.Combine(linkDir, skill);
                    if (CreateDirectorySymlink(link, target, out var message))
                    {
                        linked++;
                        log.AppendLine("  OK   " + link + "  ->  " + target);
                    }
                    else
                    {
                        failed++;
                        log.AppendLine("  FAIL " + link + "  : " + message);
                    }
                }
            }

            return $"[UnityEditorLocalization] AIエージェント連携スキルを {scopeLabel} スコープへ登録: 成功 {linked} / 失敗 {failed}\n{log}";
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

        // ディレクトリ symlink を作る。既存の実ディレクトリは壊さない（ln -sfn / mklink が安全に失敗する）。
        // macOS / Linux / Windows のいずれでも動作する。
        private static bool CreateDirectorySymlink(string link, string target, out string message)
        {
            message = "";
            try
            {
                // 登録先に「実体ディレクトリ」（symlink ではない本物のフォルダ）が既にある場合は、
                // 有効な登録（実体コピーのミラー）として扱い、何もせず成功にする。開発リポジトリでは
                // sync スクリプトがコミット済みの実体ミラーを生成しており、ここで ln -sfn を実行すると
                // 既存実ディレクトリの内部へ入れ子の symlink を作ってミラーを汚してしまうため。
                if (Directory.Exists(link) && !IsReparsePoint(link))
                    return true;

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
            sb.AppendLine("PKG=" + Quote(skillsRoot));
            sb.AppendLine("for s in " + string.Join(" ", SkillFolders) + "; do");
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
            sb.AppendLine("set \"PKG=" + pkg + "\"");
            sb.AppendLine("mkdir \"" + bse + "\\.claude\\skills\" 2>nul");
            sb.AppendLine("mkdir \"" + bse + "\\.agents\\skills\" 2>nul");
            foreach (var s in SkillFolders)
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
