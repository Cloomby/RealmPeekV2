using RealmPeek.Core.Schema;
using Realms;

namespace RealmPeek.Core.Services
{
    /// <summary>
    /// Diagnostic service to inspect specific beatmap sets/maps by GUID
    /// </summary>
    public class InspectorService
    {
        public static void Inspect(string realmPath, string setGuidStr, string mapGuidStr)
        {
            var config = new RealmConfiguration(realmPath) { IsReadOnly = true, SchemaVersion = 51 };
            using var realm = Realm.GetInstance(config);

            Console.WriteLine("\n=== INSPECTION RESULTS ===\n");

            if (!string.IsNullOrEmpty(setGuidStr) && Guid.TryParse(setGuidStr, out Guid setGuid))
            {
                InspectBeatmapSet(realm, setGuid);
            }

            if (!string.IsNullOrEmpty(mapGuidStr) && Guid.TryParse(mapGuidStr, out Guid mapGuid))
            {
                InspectBeatmap(realm, mapGuid);
            }
        }

        private static void InspectBeatmapSet(Realm realm, Guid setGuid)
        {
            var set = realm.Find<BeatmapSet>(setGuid);
            if (set == null)
            {
                Console.WriteLine($"BeatmapSet with GUID {setGuid} NOT FOUND\n");
                return;
            }

            PrintHeader("BEATMAP SET", ConsoleColor.Cyan);

            Console.WriteLine($"  ID:              {set.ID}");
            Console.WriteLine($"  OnlineID:        {set.OnlineID}");
            Console.WriteLine($"  Status:          {set.Status} ({set.Status})");
            Console.WriteLine($"  DateAdded:       {set.DateAdded}");
            Console.WriteLine($"  DateSubmitted:   {set.DateSubmitted?.ToString() ?? "NULL"}");
            Console.WriteLine($"  DateRanked:      {set.DateRanked?.ToString() ?? "NULL"}");
            Console.WriteLine($"  Hash:            {set.Hash ?? "NULL"}");
            Console.WriteLine($"  DeletePending:   {set.DeletePending}");
            Console.WriteLine($"  Protected:       {set.Protected}");
            Console.WriteLine($"  Beatmaps Count:  {set.Beatmaps.Count}");
            Console.WriteLine($"  Files Count:     {set.Files.Count}");

            if (set.Beatmaps.Any())
            {
                Console.WriteLine("\n  Beatmaps:");
                foreach (var map in set.Beatmaps)
                    Console.WriteLine($"    - {map.ID} | OnlineID: {map.OnlineID} | {map.DifficultyName} | {map.StarRating:F2}★");
            }

            if (set.Files.Any())
            {
                Console.WriteLine("\n  Files:");
                foreach (var file in set.Files.Take(10))
                    Console.WriteLine($"    - {file.Filename}");
                if (set.Files.Count > 10)
                    Console.WriteLine($"    ... and {set.Files.Count - 10} more files");
            }

            var firstMap = set.Beatmaps.FirstOrDefault();
            if (firstMap?.Metadata != null)
            {
                Console.WriteLine("\n  Metadata:");
                Console.WriteLine($"    Title:  {firstMap.Metadata.Title}");
                Console.WriteLine($"    Artist: {firstMap.Metadata.Artist}");
                Console.WriteLine($"    Author: {firstMap.Metadata.Author?.Username ?? "NULL"}");
                Console.WriteLine($"    Source: {firstMap.Metadata.Source}");
            }

            Console.WriteLine();
        }

        private static void InspectBeatmap(Realm realm, Guid mapGuid)
        {
            var map = realm.Find<Beatmap>(mapGuid);
            if (map == null)
            {
                Console.WriteLine($"Beatmap with GUID {mapGuid} NOT FOUND\n");
                return;
            }

            PrintHeader("BEATMAP", ConsoleColor.Yellow);

            Console.WriteLine($"  ID:               {map.ID}");
            Console.WriteLine($"  OnlineID:         {map.OnlineID}");
            Console.WriteLine($"  DifficultyName:   {map.DifficultyName}");
            Console.WriteLine($"  Status:           {map.Status} ({map.Status})");
            Console.WriteLine($"  Hash:             {map.Hash ?? "NULL"}");
            Console.WriteLine($"  MD5Hash:          {map.MD5Hash ?? "NULL"}");
            Console.WriteLine($"  OnlineMD5Hash:    {map.OnlineMD5Hash ?? "NULL"}");
            Console.WriteLine($"  StarRating:       {map.StarRating:F2}");
            Console.WriteLine($"  Length:           {map.Length:F0}s");
            Console.WriteLine($"  BPM:              {map.BPM:F1}");
            Console.WriteLine($"  Hidden:           {map.Hidden}");
            Console.WriteLine($"  LastLocalUpdate:  {map.LastLocalUpdate?.ToString() ?? "NULL"}");
            Console.WriteLine($"  LastOnlineUpdate: {map.LastOnlineUpdate?.ToString() ?? "NULL"}");
            Console.WriteLine($"  LastPlayed:       {map.LastPlayed?.ToString() ?? "NULL"}");

            if (map.BeatmapSet != null)
            {
                Console.WriteLine($"\n  Parent Set:");
                Console.WriteLine($"    ID:       {map.BeatmapSet.ID}");
                Console.WriteLine($"    OnlineID: {map.BeatmapSet.OnlineID}");
                Console.WriteLine($"    Status:   {map.BeatmapSet.Status}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n  ⚠️  ORPHANED BEATMAP - No parent BeatmapSet!");
                Console.ResetColor();
            }

            if (map.Metadata != null)
            {
                Console.WriteLine("\n  Metadata:");
                Console.WriteLine($"    Title:  {map.Metadata.Title}");
                Console.WriteLine($"    Artist: {map.Metadata.Artist}");
                Console.WriteLine($"    Author: {map.Metadata.Author?.Username ?? "NULL"}");
            }

            if (map.Difficulty != null)
            {
                Console.WriteLine("\n  Difficulty:");
                Console.WriteLine($"    HP:  {map.Difficulty.DrainRate}");
                Console.WriteLine($"    CS:  {map.Difficulty.CircleSize}");
                Console.WriteLine($"    OD:  {map.Difficulty.OverallDifficulty}");
                Console.WriteLine($"    AR:  {map.Difficulty.ApproachRate}");
            }
        }

        private static void PrintHeader(string title, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"--- {title} ---");
            Console.ResetColor();
        }
    }
}
