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
                summary.CustomLevelsFolderMissing = true;
                log.Warn("CustomLevels folder not found: " + customLevelsPath);
                timer.Stop();
                summary.Elapsed = timer.Elapsed;
                return summary;
            }

            List<LevelEntry> entries;
            bool hashContents = true;
            while (true)
            {
                summary.LevelsScanned = 0;
                summary.LevelsSkipped = 0;
                try
                {
                    entries = FingerprintAll(customLevelsPath, hashContents, summary, log);
                    break;
                }
                catch (ContentReadBlockedException ex) when (hashContents)
                {
                    // Another mod (e.g. SongCore on 1.44+) forbids reading beatmap files. Fall back to
                    // name+size fingerprints for the whole scan, and never delete permanently.
                    log.Warn(ex.Message);
                    log.Warn("Beatmap contents cannot be read; comparing Info.dat plus file names and sizes instead. " +
                             "Duplicates will be quarantined, never permanently deleted.");
                    hashContents = false;
                    summary.SizeOnlyMode = true;
                    options = new CleanOptions
                    {
                        DryRun = options.DryRun,
                        PermanentlyDelete = false,
                        QuarantineDirectory = options.QuarantineDirectory,
                    };
                }
            }

            List<DuplicateGroup> groups = DuplicateFinder.FindGroups(entries);
            summary.DuplicateGroups = groups.Count;

            foreach (DuplicateGroup group in groups)
            {
                log.Info("Keeping '" + group.Keeper.FolderName + "'");

                foreach (LevelEntry duplicate in group.Duplicates)
                {
                    summary.DuplicatesFound++;

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
    
        private static List<LevelEntry> FingerprintAll(
            string customLevelsPath, bool hashContents, CleanSummary summary, ILog log)
        {
            var entries = new List<LevelEntry>();
            foreach (string dir in LevelScanner.FindLevelDirectories(customLevelsPath, log))
            {
                summary.LevelsScanned++;

                string fingerprint, error;
                if (!LevelFingerprint.TryCompute(dir, hashContents, out fingerprint, out error))
                {
                    // Anything we cannot read with certainty is left alone.
                    summary.LevelsSkipped++;
                    log.Warn("Skipping " + dir + " (" + error + ")");
                    continue;
                }

                entries.Add(new LevelEntry(dir, fingerprint, Directory.GetCreationTimeUtc(dir), LevelNaming.TryReadNames(dir)));
            }
            return entries;
        }
    }
}
