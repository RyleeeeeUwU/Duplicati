using System;
using System.Collections.Generic;
using System.IO;

namespace Duplicati.Core
{
    public static class LevelScanner
    {
        /// <summary>
        /// Finds every level folder (a folder that directly contains Info.dat) below
        /// <paramref name="root"/>. SongCore allows nested sub-folders inside CustomLevels, so this
        /// recurses through folders that are not levels themselves, but never descends into a level.
        /// Symlinks/junctions below the root are skipped to avoid loops and to never delete through a link.
        /// </summary>
        public static IEnumerable<string> FindLevelDirectories(string root, ILog log)
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string dir = pending.Pop();

                bool isRoot = string.Equals(dir, root, StringComparison.Ordinal);
                if (!isRoot && LevelFingerprint.FindInfoFile(dir) != null)
                {
                    yield return dir;
                    continue;
                }

                string[] children;
                try
                {
                    children = Directory.GetDirectories(dir);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    log.Warn("Cannot read folder " + dir + ": " + ex.Message);
                    continue;
                }

                foreach (string child in children)
                {
                    try
                    {
                        if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                        {
                            log.Info("Skipping linked folder: " + child);
                            continue;
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        log.Warn("Cannot inspect folder " + child + ": " + ex.Message);
                        continue;
                    }

                    pending.Push(child);
                }
            }
        }
    }
}
