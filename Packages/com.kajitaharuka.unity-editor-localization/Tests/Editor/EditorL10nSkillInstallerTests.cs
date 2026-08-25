#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Kajitaharuka.EditorLocalization.Tests
{
    /// <summary>
    /// スキル登録の「既に登録済みのときに内容が更新されるか」を検査します。
    /// </summary>
    /// <remarks>
    /// 実体ディレクトリがある登録先を「成功」とだけ返して何もしないと、古い内容が残ったまま
    /// 更新済みに見えます（実際に踏んだ）。この振る舞いをテストで固定します。
    /// symlink を張る経路は OS の権限に依存するためここでは検査せず、判断できる範囲だけを見ます。
    /// </remarks>
    [TestFixture]
    public sealed class EditorL10nSkillInstallerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "uel-skill-installer-tests-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void DirectoriesMatch_SameFiles_ReturnsTrue()
        {
            var source = CreateSource();
            var dest = CreateDir("dest");
            Write(dest, "SKILL.md", "本文");
            Write(dest, "references/notes.md", "参照");

            Assert.That(EditorL10nSkillInstaller.DirectoriesMatch(source, dest), Is.True);
        }

        [Test]
        public void DirectoriesMatch_IgnoresMetaFilesOnTheSourceSide()
        {
            var source = CreateSource();
            Write(source, "SKILL.md.meta", "guid: 1");
            var dest = CreateDir("dest");
            Write(dest, "SKILL.md", "本文");
            Write(dest, "references/notes.md", "参照");

            // ミラーはエージェント用なので *.meta を持たない。持たないことを不一致にしてはいけない。
            Assert.That(EditorL10nSkillInstaller.DirectoriesMatch(source, dest), Is.True);
        }

        [Test]
        public void DirectoriesMatch_StaleContent_ReturnsFalse()
        {
            var source = CreateSource();
            var dest = CreateDir("dest");
            Write(dest, "SKILL.md", "古い本文");
            Write(dest, "references/notes.md", "参照");

            Assert.That(EditorL10nSkillInstaller.DirectoriesMatch(source, dest), Is.False);
        }

        [Test]
        public void DirectoriesMatch_ExtraFileInDestination_ReturnsFalse()
        {
            var source = CreateSource();
            var dest = CreateDir("dest");
            Write(dest, "SKILL.md", "本文");
            Write(dest, "references/notes.md", "参照");
            Write(dest, "references/removed.md", "正本から消えた資料");

            Assert.That(EditorL10nSkillInstaller.DirectoriesMatch(source, dest), Is.False);
        }

        [Test]
        public void EnsureSkillEntry_RealDirectoryWithStaleContent_UpdatesItInPlace()
        {
            var source = CreateSource();
            var dest = CreateDir("dest");
            Write(dest, "SKILL.md", "古い本文");
            Write(dest, "references/removed.md", "正本から消えた資料");

            var action = EditorL10nSkillInstaller.EnsureSkillEntry(dest, source, out var message);

            Assert.That(action, Is.EqualTo(EditorL10nSkillEntryAction.Copied), message);
            Assert.That(File.ReadAllText(Path.Combine(dest, "SKILL.md")), Is.EqualTo("本文"));
            Assert.That(File.Exists(Path.Combine(dest, "references", "notes.md")), Is.True, "正本の新しいファイルが入る");
            Assert.That(File.Exists(Path.Combine(dest, "references", "removed.md")), Is.False, "正本に無いファイルは消える");
            Assert.That(EditorL10nSkillInstaller.DirectoriesMatch(source, dest), Is.True);
        }

        [Test]
        public void EnsureSkillEntry_RealDirectoryAlreadyCurrent_ReportsNoChange()
        {
            var source = CreateSource();
            var dest = CreateDir("dest");
            Write(dest, "SKILL.md", "本文");
            Write(dest, "references/notes.md", "参照");

            var action = EditorL10nSkillInstaller.EnsureSkillEntry(dest, source, out var message);

            Assert.That(action, Is.EqualTo(EditorL10nSkillEntryAction.AlreadyCurrent), message);
        }

        [Test]
        public void EnsureSkillEntry_MissingSource_Fails()
        {
            var dest = CreateDir("dest");
            Write(dest, "SKILL.md", "古い本文");

            var action = EditorL10nSkillInstaller.EnsureSkillEntry(dest, Path.Combine(_root, "does-not-exist"), out _);

            Assert.That(action, Is.EqualTo(EditorL10nSkillEntryAction.Failed));
        }

        [Test]
        public void GetSkillFolders_EnumeratesEverySkillShippedWithThePackage()
        {
            var folders = EditorL10nSkillInstaller.GetSkillFolders();

            // 固定配列を手で増やし忘れても、同梱スキルが登録対象から漏れないことを担保する。
            Assert.That(folders, Is.Not.Empty);
            Assert.That(folders, Contains.Item("editor-localization-translation-quality"));
            Assert.That(folders, Contains.Item("editor-localization-optional-integration"));
            Assert.That(folders.Distinct().Count(), Is.EqualTo(folders.Count), "重複が無い");
        }

        [Test]
        public void NormalizeSeparators_MakesPackageCacheDetectionPlatformIndependent()
        {
            var windowsPath = "D:" + Path.DirectorySeparatorChar + "proj" +
                              Path.DirectorySeparatorChar + "Library" +
                              Path.DirectorySeparatorChar + "PackageCache" +
                              Path.DirectorySeparatorChar + "pkg@1" +
                              Path.DirectorySeparatorChar + "skills";

            Assert.That(EditorL10nSkillInstaller.NormalizeSeparators(windowsPath),
                Does.Contain("/Library/PackageCache/"));
        }

        // 正本に見立てたフォルダ（入れ子を含む）を作る。
        private string CreateSource()
        {
            var source = CreateDir("source");
            Write(source, "SKILL.md", "本文");
            Write(source, "references/notes.md", "参照");
            return source;
        }

        private string CreateDir(string name)
        {
            var path = Path.Combine(_root, name);
            Directory.CreateDirectory(path);
            return path;
        }

        private static void Write(string root, string relative, string content)
        {
            var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
    }
}
#endif
