using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Duplicati.Core
{
    /// <summary>How a BeatSaver-style folder name orders song and mapper.</summary>
    public enum NamingScheme
    {
        /// <summary>Cannot tell (not BeatSaver-style, no song name, or the order is ambiguous).</summary>
        Unknown = 1,
        /// <summary>Old scheme: "key (Mapper(s) - Song Title)". Lowest keeper preference.</summary>
        Old = 0,
        /// <summary>Modern scheme: "key (Song Title - Mapper(s))". Highest keeper preference.</summary>
        Modern = 2,
    }

    public static class LevelNaming
    {
        private static readonly Regex KeyedFolder =
            new Regex(@"^[0-9a-f]{1,6} \((?<inner>.+)\)$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Reads the song title and mapper names from Info.dat. v2/v3: "_songName" and "_levelAuthorName";
        /// v4: "song.title" and "difficultyBeatmaps[].beatmapAuthors.mappers[]". Never throws.
        /// </summary>
        public static LevelNameInfo TryReadNames(string levelDir)
        {
            var result = new LevelNameInfo();
            try
            {
                string info = LevelFingerprint.FindInfoFile(levelDir);
                if (info == null)
                    return result;

                byte[] bytes = File.ReadAllBytes(info);
                using (var reader = new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false), true))
                {
                    JObject root = JObject.Parse(reader.ReadToEnd());

                    string song = (string)root["_songName"] ?? (string)root.SelectToken("song.title");
                    if (!string.IsNullOrWhiteSpace(song))
                        result.SongName = song;

                    string author = (string)root["_levelAuthorName"];
                    if (!string.IsNullOrWhiteSpace(author))
                        result.Mappers.Add(author);

                    JToken diffs = root["difficultyBeatmaps"];
                    if (diffs != null)
                        foreach (JToken mapper in diffs.SelectTokens("[*].beatmapAuthors.mappers[*]"))
                        {
                            string name = (string)mapper;
                            if (!string.IsNullOrWhiteSpace(name) && !result.Mappers.Contains(name))
                                result.Mappers.Add(name);
                        }
                }
            }
            catch (Exception)
            {
                // Naming is only a tie-breaker; unreadable names must never abort a scan.
            }
            return result;
        }

        /// <summary>
        /// Decides whether the folder name is "key (Song - Mapper)" (modern) or "key (Mapper - Song)" (old).
        /// First signal: which end of the parenthesised part matches the song title. If that is
        /// inconclusive (title in the folder name differs from Info.dat), second signal: which end
        /// matches a mapper name. If the two signals disagree, the result is Unknown.
        /// Comparison ignores case and non-alphanumeric characters, because folder names have
        /// illegal path characters stripped.
        /// </summary>
        public static NamingScheme Classify(string folderName, LevelNameInfo names)
        {
            if (names == null)
                return NamingScheme.Unknown;

            Match m = KeyedFolder.Match(folderName ?? string.Empty);
            if (!m.Success)
                return NamingScheme.Unknown;

            string inner = Normalize(m.Groups["inner"].Value);

            NamingScheme bySong = Check(inner, names.SongName, songAtStartIsModern: true);

            NamingScheme byMapper = NamingScheme.Unknown;
            foreach (string mapper in names.Mappers)
            {
                NamingScheme r = Check(inner, mapper, songAtStartIsModern: false);
                if (r == NamingScheme.Unknown)
                    continue;
                if (byMapper != NamingScheme.Unknown && byMapper != r)
                {
                    byMapper = NamingScheme.Unknown; // different mappers point different ways
                    break;
                }
                byMapper = r;
            }

            if (bySong != NamingScheme.Unknown && byMapper != NamingScheme.Unknown && bySong != byMapper)
                return NamingScheme.Unknown;

            return bySong != NamingScheme.Unknown ? bySong : byMapper;
        }

        // For a song, matching the start means modern; for a mapper, matching the start means old.
        private static NamingScheme Check(string inner, string name, bool songAtStartIsModern)
        {
            string n = string.IsNullOrEmpty(name) ? string.Empty : Normalize(name);
            if (n.Length == 0 || inner.Length <= n.Length)
                return NamingScheme.Unknown;

            bool atStart = inner.StartsWith(n, StringComparison.Ordinal);
            bool atEnd = inner.EndsWith(n, StringComparison.Ordinal);
            if (atStart == atEnd)
                return NamingScheme.Unknown; // neither, or both (e.g. song title equals mapper name)

            bool modern = atStart == songAtStartIsModern;
            return modern ? NamingScheme.Modern : NamingScheme.Old;
        }

        private static string Normalize(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }
    }

    /// <summary>Names read from Info.dat, used only to classify folder naming.</summary>
    public sealed class LevelNameInfo
    {
        public string SongName { get; set; }
        public System.Collections.Generic.List<string> Mappers { get; private set; } =
            new System.Collections.Generic.List<string>();
    }
}
