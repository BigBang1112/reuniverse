using MyGenericHost.Models;
using System.Text.Json.Serialization;

namespace MyGenericHost;

[JsonSerializable(typeof(User))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class GitHubJsonSerializerContext : JsonSerializerContext;