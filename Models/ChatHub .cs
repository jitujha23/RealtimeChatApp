using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealtimeChatApp.Data;
using RealtimeChatApp.Models;

public class ChatHub : Hub
{
    public static HashSet<string> OnlineUsers = new();
    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }
    //public async Task SendMessage(string toUserId, string message)
    //{
    //    var fromUserId = Context.UserIdentifier;
    //    var fromUserName = Context.User.Identity.Name;

    //    // ✅ DB save
    //    _context.ChatMessages.Add(new ChatMessage
    //    {
    //        SenderId = fromUserId,
    //        ReceiverId = toUserId,
    //        Message = message
    //    });

    //    await _context.SaveChangesAsync();

    //    // ✅ receiver ko realtime message
    //    await Clients.User(toUserId)
    //        .SendAsync("ReceiveMessage", fromUserId, fromUserName, message);

    //    // ✅ notification (global)
    //    await Clients.User(toUserId)
    //        .SendAsync("NewMessageNotification", fromUserName);
    //}
    public async Task SendMessage(string toUserId, string message)
    {
        var fromUserId = Context.UserIdentifier;
        var fromUserName = Context.User.Identity.Name;

        var chat = new ChatMessage
        {
            SenderId = fromUserId,
            ReceiverId = toUserId,
            Message = message,
            MessageTime = DateTime.Now,
            IsSeen = false
        };

        _context.ChatMessages.Add(chat);
        await _context.SaveChangesAsync();

        // realtime message
        await Clients.User(toUserId)
            .SendAsync("ReceiveMessage", fromUserId, fromUserName, message);

        // ✅ unread count for B (A→B unread)
        var unreadCount = await _context.ChatMessages
            .Where(x => x.SenderId == fromUserId &&
                        x.ReceiverId == toUserId &&
                        x.IsSeen == false)
            .CountAsync();

        // receiver list update
        await Clients.User(toUserId)
            .SendAsync("UpdateUserList", new
            {
                userId = fromUserId,
                message = message,
                time = chat.MessageTime,
                unread = unreadCount
            });

        // sender list update (no unread)
        await Clients.User(fromUserId)
            .SendAsync("UpdateUserList", new
            {
                userId = toUserId,
                message = message,
                time = chat.MessageTime,
                unread = 0
            });
    }
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        Console.WriteLine("CONNECTED USER: " + userId);
        if (!string.IsNullOrEmpty(userId))
        {
            OnlineUsers.Add(userId);
            await Clients.All.SendAsync("UserOnline", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception ex)
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            OnlineUsers.Remove(userId);
            await Clients.All.SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(ex);
    }

    public async Task GetOnlineUsers()
    {
        await Clients.Caller.SendAsync("OnlineUsersList", OnlineUsers);
    }

    public async Task Typing(string toUserId)
    {
        var name = Context.User.Identity.Name;

        await Clients.User(toUserId)
            .SendAsync("UserTyping", name);
    }

    //public async Task MarkAsSeen(string senderId)
    //{
    //    var myId = Context.UserIdentifier;

    //    if (string.IsNullOrEmpty(myId))
    //        return;

    //    var msgs = await _context.ChatMessages
    //        .Where(x => x.SenderId == senderId &&
    //                    x.ReceiverId == myId &&
    //                    x.IsSeen == false)
    //        .ToListAsync();

    //    if (msgs.Any())
    //    {
    //        foreach (var msg in msgs)
    //        {
    //            msg.IsSeen = true;
    //        }

    //        await _context.SaveChangesAsync();

    //        //await Clients.User(senderId)
    //        //    .SendAsync("MessagesSeen");
    //        await Clients.User(senderId)
    //.SendAsync("MessagesSeen", myId);
    //    }
    //}

    public async Task MarkAsSeen(string senderId)
    {
        var myId = Context.UserIdentifier;

        var msgs = await _context.ChatMessages
            .Where(x => x.SenderId == senderId &&
                        x.ReceiverId == myId &&
                        x.IsSeen == false)
            .ToListAsync();

        foreach (var msg in msgs)
            msg.IsSeen = true;

        await _context.SaveChangesAsync();

        // sender ko inform karo
        await Clients.User(senderId)
            .SendAsync("MessagesSeen", myId);
    }
}
