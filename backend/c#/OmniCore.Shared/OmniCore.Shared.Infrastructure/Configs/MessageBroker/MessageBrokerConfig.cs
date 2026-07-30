namespace OmniCore.Shared.Infrastructure.Configs.MessageBroker;

using System.ComponentModel.DataAnnotations;

public class MessageBrokerConfig : ConfigBase
{
    public override string SectionName => "MessageBroker";

    [Required]
    public string Host { get; set; } = "localhost";

    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    [Range(1, 300)]
    public ushort Heartbeat { get; set; } = 10;

    [Range(1000, 60000)]
    public int RequestedConnectionTimeout { get; set; } = 30000;
}