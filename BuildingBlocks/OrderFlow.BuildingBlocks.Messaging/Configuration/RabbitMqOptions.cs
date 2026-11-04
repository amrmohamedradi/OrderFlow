namespace OrderFlow.BuildingBlocks.Messaging.Configuration;

public class RabbitMqOptions
{
    public string Host { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";

    public ushort Port { get; set; } = 5672;

    public bool UseSsl { get; set; }
}
