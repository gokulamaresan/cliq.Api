

namespace Models.Account
{
    public class User
    {
        public string email_id { get; set; }
        public string zuid { get; set; }
        public string zoid { get; set; }
        public string display_name { get; set; }
        public string name { get; set; }
        public string organization_id { get; set; }
        public string id { get; set; }
    }
    
    public class UsersResponse
    {
        public string next_token { get; set; }
        public bool has_more { get; set; }
        public List<User> data { get; set; }
    }
}