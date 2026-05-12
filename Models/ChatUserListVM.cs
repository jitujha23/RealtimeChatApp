namespace RealtimeChatApp.Models
{
    public class ChatUserListVM
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }

        public int UnreadCount { get; set; }
    }
}
