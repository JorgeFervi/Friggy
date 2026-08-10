using System.ComponentModel.DataAnnotations;

namespace Friggy.Web.Api;

public sealed class FriggyApiOptions
{
    public const string SectionName = "FriggyApi";

    [Required]
    public Uri? BaseUrl { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}
