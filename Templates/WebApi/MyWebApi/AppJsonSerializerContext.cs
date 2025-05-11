using MyWebApi.Models;
using System.Text.Json.Serialization;

namespace MyWebApi;

[JsonSerializable(typeof(User))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;