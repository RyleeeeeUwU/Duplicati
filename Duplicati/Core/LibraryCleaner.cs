using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Duplicati.Core
{
    /// <summary>Scans a CustomLevels folder and removes duplicate levels, keeping one of each.</summary>
    public static class LibraryCleaner
    {
        public static CleanSummary Run(string customLevelsPath, CleanOptions options, ILog log)
        {
            var summary = new CleanSummary();
            var timer = Stopwatch.StartNew();

            if (!Directory.Exists(customLevelsPath))
            {
                log.Warn("CustomLevels folder not found: " + customLevelsPath);
                return summary;
            }

            var entries = new List<LevelEntry>();
            foreach (string dir in LevelScanner.FindLevelDirectories(customLevelsPath, log))
            {
                summary.LevelsScanned++;

                string fingerprint, error;
                if (!LevelFingerprint.TryCompute(dir, out fingerprint, out error))
                {
                    // Anything we cannot read with certainty is left alone.
                    summary.LevelsSkipped++;
                    log.Warn("Skipping " + dir + " (" + error + ")");
                    continue;
                }

                entries.Add(new LevelEntry(dir, fingerprint, Directory.GetCreationTimeUtc(dir)));
            }

            List<DuplicateGroup> groups = DuplicateFinder.FindGroups(entries);
            summary.DuplicateGroups = groups.Count;

            foreach (DuplicateGroup group in groups)
            {
                log.Info("Keeping '" + group.Keeper.FolderName + "'");

                foreach (LevelEntry duplicate in group.Duplicates)
                {
                    if (options.DryRun)
                    {
                        log.Info("  [dry run] would remove '" + duplicate.FolderName + "'");
                        continue;
                    }

                    // Never remove a duplicate unless the copy we are keeping is still there.
                    if (!Directory.Exists(group.Keeper.Path))
                    {
                        log.Warn("  Kept copy vanished, not removing '" + duplicate.FolderName + "'");
                        summary.Failed++;
                        continue;
                    }

                    if (DuplicateRemover.TryRemove(duplicate, options, log))
                    {
                        summary.Removed++;
                        log.Info("  removed '" + duplicate.FolderName + "'");
                    }
                    else
                    {
                        summary.Failed++;
                    }
                }
            }

            timer.Stop();
            summary.Elapsed = timer.Elapsed;
            return summary;
        }
    }
}
