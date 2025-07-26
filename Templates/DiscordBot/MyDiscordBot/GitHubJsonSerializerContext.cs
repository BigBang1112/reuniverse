using MyDiscordBot.Models;
using System.Text.Json.Serialization;

namespace MyDiscordBot;

[JsonSerializable(typeof(User))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class GitHubJsonSerializerContext : JsonSerializerContext;