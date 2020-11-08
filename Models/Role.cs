using System.Collections.Generic;

namespace Display.Models
{
    public class Role
    {
        public Role(string id)
        {
            Id = id;
            Users = new List<User>();
        }

        public string Id { get; private set; }
        public IList<User> Users { get; private set; }
    }
}
