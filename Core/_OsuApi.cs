//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace RealmPeek
//{
//    public class OsuApi
//    {
//        private readonly HttpClient _client;

//        private readonly int _clientId;

//        private readonly string _clientSecret;

//        private string? _token;

//        public OsuApi(int clientId, string clientSecret)
//        {
//            _clientId = clientId;

//            _clientSecret = clientSecret;

//            _client = new HttpClient { BaseAddress = new Uri("https://osu.ppy.sh/api/v2/") };
//        }

//        public async Task<bool> AuthenticateAsync()
//        {
//            var request = new
//            {
//                client_id = _clientId,

//                client_secret = _clientSecret,

//                grant_type = "client_credentials",

//                scope = "public",
//            };

//            var response = await _client.PostAsJsonAsync("https://osu.ppy.sh/oauth/token", request);

//            if (!response.IsSuccessStatusCode)
//            {
//                return false;
//            }

//            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

//            _token = json.GetProperty("access_token").GetString();

//            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

//            return true;
//        }

//        // CHANGED: Fetch 'Beatmaps' (Maps) instead of 'BeatmapSets' (Sets)

//        // This endpoint supports the bulk ?ids[]= query.

//        public async Task<List<ApiBeatmap>> GetBeatmapsAsync(IEnumerable<int> ids)
//        {
//            var validIds = ids.Where(x => x > 0).ToList();

//            if (!validIds.Any())
//            {
//                return new List<ApiBeatmap>();
//            }

//            var queryString = "?" + string.Join("&", validIds.Select(x => $"ids[]={x}"));

//            var response = await _client.GetAsync("beatmaps" + queryString);

//            if (!response.IsSuccessStatusCode)
//            {
//                Console.WriteLine($"API Error [{response.StatusCode}]");

//                return new List<ApiBeatmap>();
//            }

//            var container = await response.Content.ReadFromJsonAsync<ApiMapContainer>();

//            return container?.Beatmaps ?? new List<ApiBeatmap>();
//        }
//    }

//    // --- UPDATED DTOs ---

//    public class ApiMapContainer
//    {
//        [JsonPropertyName("beatmaps")]
//        public List<ApiBeatmap>? Beatmaps { get; set; }
//    }

//    public class ApiBeatmap
//    {
//        [JsonPropertyName("id")]
//        public int Id { get; set; }

//        [JsonPropertyName("checksum")]
//        public string? MD5Hash { get; set; }

//        // The API nests the Set info INSIDE the Map info

//        [JsonPropertyName("beatmapset")]
//        public ApiBeatmapSet? BeatmapSet { get; set; }
//    }

//    public class ApiBeatmapSet
//    {
//        [JsonPropertyName("id")]
//        public int Id { get; set; }

//        // API returns status as a string ("ranked", "loved", etc.), not an int!

//        [JsonPropertyName("status")]
//        public string? Status { get; set; }

//        [JsonPropertyName("last_updated")]
//        public DateTimeOffset LastUpdated { get; set; }

//        [JsonPropertyName("ranked_date")]
//        public DateTimeOffset? RankedDate { get; set; }

//        [JsonPropertyName("submitted_date")]
//        public DateTimeOffset? SubmittedDate { get; set; }
//    }
//}
