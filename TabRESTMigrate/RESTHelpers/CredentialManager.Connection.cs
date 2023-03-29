partial class CredentialManager
{
    internal class Connection
    {
        public readonly string ServerAddress;

        public readonly string ServerPort;

        public Connection(string serverAddress, string serverPort = null)
        {
            this.ServerAddress = serverAddress;
            this.ServerPort = serverPort;
        }
    }

}
