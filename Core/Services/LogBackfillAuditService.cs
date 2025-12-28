using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RealmPeek.Core.Data;
using RealmPeek.Core.Infrastructure;
using RealmPeek.Core.Models;

namespace RealmPeek.Core.Services
{
    public partial class LogBackfillAuditService
    {
        // A lightweight record to hold the structured data
        public record MapEntry(string Title, string Artist, string? Author);

        [GeneratedRegex(@"Could not find (?<Artist>.+?) - (?<Title>.+?)(?: \((?<Author>[^)]+)\))? in local cache")]
        private static partial Regex BackfillRegex();

        public List<MapEntry> ParseLogFile(string logFilePath)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                Console.WriteLine("No logfile provided!");
                return []; // Return empty list (C# 12 syntax)
            }

            // Fix: Added '!' to check if file does NOT exist
            if (!File.Exists(logFilePath))
            {
                Console.WriteLine($"Provided logfile not found: {logFilePath}");
                return []; // Return empty list
            }

            List<MapEntry> results = new List<MapEntry>();
            var regex = BackfillRegex();

            // Use ReadLines to stream the file (better memory profile than ReadAllLines)
            foreach (var line in File.ReadLines(logFilePath))
            {
                // Skip lines early if they clearly dont match the context
                if (!line.Contains("in local cache"))
                    continue;

                var match = regex.Match(line);
                if (match.Success)
                {
                    results.Add(
                        new MapEntry(
                            Title: match.Groups["Title"].Value,
                            Artist: match.Groups["Artist"].Value,
                            // If the group wasn't matched, return null so JSON serializes it as "null"
                            Author: match.Groups["Author"].Success ? match.Groups["Author"].Value : null
                        )
                    );
                }
            }

            return results;
        }

        public void StartBackfillAudit(string logFilePath, string realmFilePath)
        {
            List<MapEntry> results = ParseLogFile(logFilePath);
            var realmPath = FilePathHelper.GetFullPath(realmFilePath);
            var index = 0;
            var count = 0;

            if (results.Count > 0)
            {
                foreach (var beatmap in results)
                {
                    var repo = new RealmRepository(realmPath);
                    BeatmapModel beatmapResult = repo.FindBeatmapByQuery("(Metadata.Title == $0 AND Metadata.Artist == $1) OR Metadata.Author.Username == $2", beatmap.Title, beatmap.Artist, beatmap.Author);

                    if (beatmapResult != null)
                    {
                        Console.WriteLine($"AYO! Beatmap #{index}: {beatmap.Artist} - {beatmap.Title} ({beatmap.Author}) was FOUND!");
                        count++;
                    }
                    else
                    {
                        Console.WriteLine($"Sadge... Beatmap #{index}: {beatmap.Artist} - {beatmap.Title} ({beatmap.Author}) was not found...");
                    }
                    index++;
                }

                Console.WriteLine($"Found {count}/{results.Count} maps");
            }
        }
    }
}
