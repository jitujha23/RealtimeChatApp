namespace RealtimeChatApp.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public string SenderId { get; set; }
        public string ReceiverId { get; set; }

        public string? Message { get; set; }

        public DateTime MessageTime { get; set; } = DateTime.Now;
        public bool IsSeen { get; set; } = false;
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
    }
}
