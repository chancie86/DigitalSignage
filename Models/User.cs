namespace Display.Models
{
    public class User
    {
        public User(string username, string domain)
        {
            Username = username;
            Domain = domain;
        }

        public User(string username)
            : this(username, null)
        {
        }

        public string Username { get; private set; }
        public string Domain { get; private set; }
    }
}
