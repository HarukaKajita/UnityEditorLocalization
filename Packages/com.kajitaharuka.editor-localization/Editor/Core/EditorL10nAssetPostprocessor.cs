#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Kajitaharuka.EditorLocalization
{
    internal sealed class EditorL10nAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var changedJsonPaths = EnumerateJsonPaths(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths).ToArray();
            if (changedJsonPaths.Length == 0)
                return;

            if (!ContainsLocalizationPath(changedJsonPaths))
                return;

            EditorL10n.Reload();
        }

        private static IEnumerable<string> EnumerateJsonPaths(params string[][] assetPathGroups)
        {
            foreach (var group in assetPathGroups ?? Array.Empty<string[]>())
            {
                foreach (var path in group ?? Array.Empty<string>())
                {
                    var normalizedPath = NormalizeAssetPath(path);
                    if (normalizedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        yield return normalizedPath;
                }
            }
        }

        private static bool ContainsLocalizationPath(IReadOnlyCollection<string> changedJsonPaths)
        {
            if (changedJsonPaths.Any(EditorL10nCatalog.IsManifestPath))
                return true;

            var tablePaths = new HashSet<string>(
                EditorL10n.Catalog.Scopes.SelectMany(scope => scope.TablePaths),
                StringComparer.Ordinal);

            return changedJsonPaths.Any(tablePaths.Contains);
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? "").Replace("\\", "/");
        }
    }
}
#endif
