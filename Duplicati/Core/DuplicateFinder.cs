using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Duplicati.Core
{
    public static class DuplicateFinder
    {
        // BeatSaver downloads are named "<hex key> (<song> - <mapper>)", e.g. "2ab3f (Song - Mapper)".
        // Lowercase hex only, so ordinary titles like "Faded (Alan Walker)" don't match.
        // This is only a tie-breaker, never a reason to treat levels as duplicates.
        private static readonly Regex BeatSaverFolder =
            new Regex(@"^[0-9a-f]{1,6} \(.+\)$", RegexOptions.CultureInvariant);

        public static bool LooksLikeBeatSaverFolder(string folderName)
        {
            return BeatSaverFolder.IsMatch(folderName ?? string.Empty);
        }

        /// <summary>
        /// Groups levels by fingerprint and picks the one to keep in each group of two or more.
        /// Keeper preference: BeatSaver-style folder name, then the oldest folder, then path order
        /// (so the result is deterministic).
        /// </summary>
        public static List<DuplicateGroup> FindGroups(IEnumerable<LevelEntry> entries)
        {
            var groups = new List<DuplicateGroup>();

            foreach (var group in entries.GroupBy(e => e.Fingerprint, StringComparer.Ordinal))
            {
                List<LevelEntry> ordered = group
                    .OrderByDescending(e => LooksLikeBeatSaverFolder(e.FolderName))
                    .ThenBy(e => e.CreatedUtc)
                    .ThenBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (ordered.Count < 2)
                    continue;

                groups.Add(new DuplicateGroup(group.Key, ordered[0], ordered.Skip(1).ToList()));
            }

            return groups
                .OrderBy(g => g.Keeper.FolderName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
