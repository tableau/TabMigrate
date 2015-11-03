partial class CredentialManager
{
    internal class Credential
    {
        public readonly string Name;
        public readonly string Password;
        public readonly bool IsEmbedded;

        public Credential(string name, string password, bool isEmbedded)
        {
            this.Name = name;
            this.Password = password;
            this.IsEmbedded = isEmbedded;
        }
    }

}
