using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Duplicati.Core
{
    /// <summary>
    /// Computes a content fingerprint for a custom level folder.
    ///
    /// Two folders get the same fingerprint when their Info.dat and every beatmap/lightshow file
    /// it references are byte-identical, regardless of how the folders are named. Audio and cover
    /// images are deliberately not hashed (they are large and Info.dat already pins the chart).
    ///
    /// This follows the same idea as the BeatSaver map hash but is NOT guaranteed to equal it
    /// (the info format changed in v4), so never compare it against BeatSaver hashes.
    /// </summary>
    public static class LevelFingerprint
    {
        // Property names in Info.dat that point at other files in the level folder.
        //  v2/v3 info: _beatmapFilename
        //  v4 info:    beatmapDataFilename, lightshowDataFilename, audioDataFilename
        private static readonly HashSet<string> FileKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "_beatmapFilename",
            "beatmapDataFilename",
            "lightshowDataFilename",
            "audioDataFilename",
        };

        public static string FindInfoFile(string levelDir)
        {
            // Beat Saber on Windows is case-insensitive; check both for Wine/Linux setups.
            foreach (string name in new[] { "Info.dat", "info.dat" })
            {
                string candidate = Path.Combine(levelDir, name);
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        /// <summary>
        /// Returns false (with a reason) if the level cannot be fingerprinted reliably.
        /// Such levels must never be treated as duplicates.
        /// </summary>
        public static bool TryCompute(string levelDir, out string fingerprint, out string error)
        {
            fingerprint = null;
            error = null;

            string infoPath = FindInfoFile(levelDir);
            if (infoPath == null)
            {
                error = "no Info.dat";
                return false;
            }

            try
            {
                byte[] infoBytes = File.ReadAllBytes(infoPath);
                List<string> referenced = CollectReferencedFiles(infoBytes);
                string levelRoot = Path.GetFullPath(levelDir)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                using (SHA1 sha = SHA1.Create())
                {
                    Feed(sha, infoBytes);

                    foreach (string name in referenced)
                    {
                        string full = Path.GetFullPath(Path.Combine(levelDir, name));

                        if (!full.StartsWith(levelRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            // Info.dat points outside its own folder. Don't read it; mark it so
                            // such a level can only match one with the exact same bad reference.
                            Feed(sha, Encoding.UTF8.GetBytes("\0outside:" + name));
                        }
                        else if (!File.Exists(full))
                        {
                            Feed(sha, Encoding.UTF8.GetBytes("\0missing:" + name));
                        }
                        else
                        {
                            Feed(sha, Encoding.UTF8.GetBytes("\0file:" + name + "\0"));
                            FeedFile(sha, full);
                        }
                    }

                    sha.TransformFinalBlock(new byte[0], 0, 0);
                    fingerprint = BitConverter.ToString(sha.Hash).Replace("-", string.Empty);
                }

                return true;
            }
            catch (Exception ex) when (
                ex is IOException || ex is UnauthorizedAccessException ||
                ex is JsonException || ex is ArgumentException || ex is NotSupportedException)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static List<string> CollectReferencedFiles(byte[] infoBytes)
        {
            JToken root;
            // StreamReader with BOM detection: some editors save Info.dat with a UTF-8 BOM.
            using (var reader = new StreamReader(new MemoryStream(infoBytes), new UTF8Encoding(false), true))
            using (var json = new JsonTextReader(reader))
            {
                root = JToken.ReadFrom(json);
            }

            var result = new List<string>();
            Walk(root, result);
            return result;
        }

        // Document order is preserved, which keeps the hash deterministic.
        private static void Walk(JToken token, List<string> result)
        {
            var obj = token as JObject;
            if (obj != null)
            {
                foreach (JProperty prop in obj.Properties())
                {
                    if (prop.Value.Type == JTokenType.String && FileKeys.Contains(prop.Name))
                    {
                        string value = (string)prop.Value;
                        if (!string.IsNullOrWhiteSpace(value))
                            result.Add(value);
                    }
                    else
                    {
                        Walk(prop.Value, result);
                    }
                }
                return;
            }

            var array = token as JArray;
            if (array != null)
            {
                foreach (JToken item in array)
                    Walk(item, result);
            }
        }

        private static void Feed(HashAlgorithm hash, byte[] data)
        {
            hash.TransformBlock(data, 0, data.Length, null, 0);
        }

        private static void FeedFile(HashAlgorithm hash, string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    hash.TransformBlock(buffer, 0, read, null, 0);
            }
        }
    }
}
