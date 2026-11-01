using System;
using System.IO;

namespace Duplicati.Core
{
    public static class DuplicateRemover
    {
        /// <summary>
        /// Removes one duplicate level folder: moves it to quarantine, or deletes it outright.
        /// Returns false (and logs) on failure; never throws for ordinary IO problems.
        /// </summary>
        public static bool TryRemove(LevelEntry duplicate, CleanOptions options, ILog log)
        {
            try
            {
                if (options.PermanentlyDelete)
                {
                    DeleteDirectory(duplicate.Path);
                }
                else
                {
                    if (string.IsNullOrEmpty(options.QuarantineDirectory))
                        throw new InvalidOperationException("No quarantine directory configured.");

                    Directory.CreateDirectory(options.QuarantineDirectory);
                    string destination = UniqueDestination(options.QuarantineDirectory, duplicate.FolderName);
                    MoveDirectory(duplicate.Path, destination);
                }

                return true;
            }
            catch (Exception ex) when (
                ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                log.Warn("Could not remove " + duplicate.Path + ": " + ex.Message);
                return false;
            }
        }

        private static string UniqueDestination(string quarantine, string folderName)
        {
            string candidate = Path.Combine(quarantine, folderName);
            int n = 2;
            while (Directory.Exists(candidate) || File.Exists(candidate))
            {
                candidate = Path.Combine(quarantine, folderName + " (" + n + ")");
                n++;
            }
            return candidate;
        }

        private static void MoveDirectory(string source, string destination)
        {
            try
            {
                Directory.Move(source, destination);
            }
            catch (IOException)
            {
                // Directory.Move cannot cross volumes on .NET Framework. Fall back to copy + delete,
                // and only delete the source once the copy fully succeeded.
                try
                {
                    CopyDirectory(source, destination);
                }
                catch
                {
                    TryDeleteQuietly(destination);
                    throw;
                }

                try
                {
                    DeleteDirectory(source);
                }
                catch
                {
                    // Source is locked or read-only: undo the copy so a retry on the next launch
                    // doesn't pile up extra quarantine copies.
                    TryDeleteQuietly(destination);
                    throw;
                }
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), false);

            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }

        private static void DeleteDirectory(string path)
        {
            // Read-only files make Directory.Delete throw on .NET Framework; clear the flag first.
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);

            Directory.Delete(path, true);
        }

        private static void TryDeleteQuietly(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    DeleteDirectory(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Best effort cleanup of a partial copy.
            }
        }
    }
}
