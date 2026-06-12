namespace OrderProcessor.Infrastructure.Notifications
{
    public class EmailOptions
    {
        public const string SectionName = "EmailSettings";
        public string Mail { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Host { get; set; } = null!;
        public int Port { get; set; }
    }
}