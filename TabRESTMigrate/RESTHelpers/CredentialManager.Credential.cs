partial class CredentialManager
{
    internal class Credential
    {
        public readonly string Name;
        public readonly string Password;
        public readonly bool IsEmbedded;
        public readonly Connection Connection;

        public Credential(string name, string password, bool isEmbedded, Connection connection = null)
        {
            this.Name = name;
            this.Password = password;
            this.IsEmbedded = isEmbedded;
            this.Connection = connection;
        }
    }

}
