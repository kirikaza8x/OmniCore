namespace OmniCore.Services.Auth.Infrastructure.Configs;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

public class GoogleAuthConfig : ConfigBase
{
    public override string SectionName => "GoogleAuth";

    [Required(ErrorMessage = "Google ServerClientId is required.")]
    public string ServerClientId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;
}