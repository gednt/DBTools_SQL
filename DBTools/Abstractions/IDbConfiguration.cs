namespace DBTools.Abstractions
{
    public interface IDbConfiguration
    {
        string Host { get; }
        string Database { get; }
        string Uid { get; }
        string Password { get; }
        string Port { get; }
        string ConnectionString { get; }
    }
}
