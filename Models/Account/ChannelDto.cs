namespace Models.ChannelDto
{
    public class ChannelsResponse
    {
        public List<Channel> channels { get; set; }
    }

    public class Channel
    {
        public bool pinned { get; set; }
        public string level { get; set; }
        public string chat_id { get; set; }
        public bool joined { get; set; }
        public string creator_name { get; set; }
        public string unique_name { get; set; }
        public string total_message_count { get; set; }
        public string organization_id { get; set; }
        public string channel_id { get; set; }
        public string creator_id { get; set; }
        public bool invite_only { get; set; }
        public int muted_interval { get; set; }
    }
}
