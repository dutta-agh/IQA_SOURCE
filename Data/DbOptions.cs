namespace YourApp.Data
{
    public sealed class DbOptions
    {
        public string Server { get; set; } = string.Empty;
        public uint Port { get; set; } = 3306;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public bool Ssl { get; set; } = true;
        public int ConnectTimeoutSeconds { get; set; } = 15;
        public int MinPoolSize { get; set; } = 0;
        public int MaxPoolSize { get; set; } = 100;
    }
}