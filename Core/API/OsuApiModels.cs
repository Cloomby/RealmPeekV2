using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RealmPeek.Core.API
{
    /// <summary>
    /// Response container for /beatmaps endpoint
    /// API returns: { "beatmaps": [ ... ] }
    /// </summary>
    public class BeatmapsResponse
    {
        [JsonPropertyName("beatmaps")]
        public List<ApiBeatmap> Beatmaps { get; set; } = new();
    }

    /// <summary>
    /// Individual beatmap (difficulty) from API
    /// Endpoint: GET /beatmaps?ids[]=X
    /// </summary>
    public class ApiBeatmap
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("beatmapset_id")]
        public int BeatmapSetId { get; set; }

        [JsonPropertyName("difficulty_rating")]
        public double DifficultyRating { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("total_length")]
        public int TotalLength { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("accuracy")]
        public float Accuracy { get; set; }

        [JsonPropertyName("ar")]
        public float AR { get; set; }

        [JsonPropertyName("bpm")]
        public double BPM { get; set; }

        [JsonPropertyName("cs")]
        public float CS { get; set; }

        [JsonPropertyName("drain")]
        public float Drain { get; set; }

        [JsonPropertyName("hit_length")]
        public int HitLength { get; set; }

        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        [JsonPropertyName("beatmapset")]
        public ApiBeatmapSet? BeatmapSet { get; set; }

        [JsonPropertyName("max_combo")]
        public int? MaxCombo { get; set; }

        public override string ToString() => $"{BeatmapSetId}/{Id} - {Version} [{DifficultyRating:F2}★]";
    }

    /// <summary>
    /// Beatmap set from API (nested in ApiBeatmap)
    /// </summary>
    public class ApiBeatmapSet
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonPropertyName("artist_unicode")]
        public string ArtistUnicode { get; set; } = string.Empty;

        [JsonPropertyName("creator")]
        public string Creator { get; set; } = string.Empty;

        [JsonPropertyName("favourite_count")]
        public int FavouriteCount { get; set; }

        [JsonPropertyName("nsfw")]
        public bool NSFW { get; set; }

        [JsonPropertyName("play_count")]
        public int PlayCount { get; set; }

        [JsonPropertyName("preview_url")]
        public string PreviewUrl { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("title_unicode")]
        public string TitleUnicode { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("video")]
        public bool Video { get; set; }

        // Dates
        [JsonPropertyName("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonPropertyName("ranked_date")]
        public DateTimeOffset? RankedDate { get; set; }

        [JsonPropertyName("submitted_date")]
        public DateTimeOffset? SubmittedDate { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; } = string.Empty;

        public override string ToString() => $"{Artist} - {Title} (by {Creator})";
    }

    /// <summary>
    /// OAuth2 token response
    /// Endpoint: POST /oauth/token
    /// </summary>
    public class OAuthTokenResponse
    {
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// API error response
    /// </summary>
    public class ApiErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
