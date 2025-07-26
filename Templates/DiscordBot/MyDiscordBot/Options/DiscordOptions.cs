using System.ComponentModel.DataAnnotations;

namespace MyDiscordBot.Options;

public sealed class DiscordOptions
{
    public const string Discord = "Discord";

    [Required]
    public required string Token { get; set; }

    [Required]
    public required string TestGuildId { get; set; }
}
