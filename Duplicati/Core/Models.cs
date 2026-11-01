using System;
using System.Collections.Generic;
using System.IO;

namespace Duplicati.Core
{
    /// <summary>A single custom level folder together with its content fingerprint.</summary>
    public sealed class LevelEntry
    {
        public LevelEntry(string path, string fingerprint, DateTime createdUtc)
        {
            Path = path;
            Fingerprint = fingerprint;
            CreatedUtc = createdUtc;
            FolderName = System.IO.Path.GetFileName(
                path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        }

        public string Path { get; private set; }
        public string FolderName { get; private set; }
        public string Fingerprint { get; private set; }
        public DateTime CreatedUtc { get; private set; }
    }

    /// <summary>A set of levels with identical content: one to keep, the rest to remove.</summary>
    public sealed class DuplicateGroup
    {
        public DuplicateGroup(string fingerprint, LevelEntry keeper, List<LevelEntry> duplicates)
        {
            Fingerprint = fingerprint;
            Keeper = keeper;
            Duplicates = duplicates;
        }

        public string Fingerprint { get; private set; }
        public LevelEntry Keeper { get; private set; }
        public List<LevelEntry> Duplicates { get; private set; }
    }

    public sealed class CleanOptions
    {
        /// <summary>Only log what would be removed; touch nothing.</summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// When false (default) duplicates are moved into <see cref="QuarantineDirectory"/>
        /// instead of being deleted, so a wrong call can be undone.
        /// </summary>
        public bool PermanentlyDelete { get; set; }

        /// <summary>Where duplicates are moved when <see cref="PermanentlyDelete"/> is false.</summary>
        public string QuarantineDirectory { get; set; }
    }

    public sealed class CleanSummary
    {
        public int LevelsScanned { get; set; }
        public int LevelsSkipped { get; set; }
        public int DuplicateGroups { get; set; }
        public int Removed { get; set; }
        public int Failed { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
