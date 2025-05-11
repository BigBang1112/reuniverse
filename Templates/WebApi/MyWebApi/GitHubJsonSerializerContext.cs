using MyWebApi.Models;
using System.Text.Json.Serialization;

namespace MyWebApi;

[JsonSerializable(typeof(User))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class GitHubJsonSerializerContext : JsonSerializerContext;