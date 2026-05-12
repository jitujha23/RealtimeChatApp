using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealtimeChatApp.Data;
using RealtimeChatApp.Models;

[Authorize]
public class ChatController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IWebHostEnvironment _env;

    public ChatController(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context,
        IHubContext<ChatHub> hub,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _context = context;
        _hub = hub;
        _env = env;
    }

    // 👥 USER LIST
    public IActionResult Index()
    {
        var myId = _userManager.GetUserId(User);

        var users = _context.Users
            .Where(u => u.Id != myId)
            .ToList();

        return View(users);
    }

    // 💬 CHAT WINDOW
    public async Task<IActionResult> ChatWindow(string userId)
    {
        var myId = _userManager.GetUserId(User);
        var messages = await _context.ChatMessages
       .Where(x => (x.SenderId == myId && x.ReceiverId == userId)
                || (x.SenderId == userId && x.ReceiverId == myId))
       .OrderBy(x => x.MessageTime)
       .ToListAsync();

        var user = await _userManager.FindByIdAsync(userId);

        ViewBag.ReceiverId = userId;
        ViewBag.ReceiverName = user.UserName;
        ViewBag.Messages = messages;

        return View();
    }

    // 🚀 SEND MESSAGE + FILE (SINGLE API)
    [HttpPost]
    public async Task<IActionResult> SendMessage(string message, string receiver, IFormFile file)
    {
        var myId = _userManager.GetUserId(User);
        var myName = User.Identity.Name;

        string filePath = null;
        string fileName = null;

        // 📁 FILE SAVE
        if (file != null && file.Length > 0)
        {
            fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            var uploadPath = Path.Combine(_env.WebRootPath, "Uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fullPath = Path.Combine(uploadPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            filePath = "/Uploads/" + fileName;
        }

        // ❗ VALIDATION → message ya file me se ek required
        if (string.IsNullOrEmpty(message) && filePath == null)
            return BadRequest();

        // 💾 SAVE IN DATABASE
        var chat = new ChatMessage
        {
            SenderId = myId,
            ReceiverId = receiver,
            Message = message ?? "",
            FilePath = filePath,
            FileName = fileName,
            MessageTime = DateTime.Now,
            IsSeen = false
        };

        _context.ChatMessages.Add(chat);
        await _context.SaveChangesAsync();

        // ⚡ REALTIME SEND
        await _hub.Clients.User(receiver)
            .SendAsync("ReceiveMessage",
                        myId,
                        myName,
                        message,
                        filePath,
                        fileName);

        return Ok();
    }


    public IActionResult IndexMain()
    {
        var myId = _userManager.GetUserId(User);

        var users = _context.Users
            .Where(u => u.Id != myId)
            .ToList();

        return View(users);
    }
    public async Task<IActionResult> ChatWindowMain(string userId)
    {
        var myId = _userManager.GetUserId(User);
        var messages = await _context.ChatMessages
       .Where(x => (x.SenderId == myId && x.ReceiverId == userId)
                || (x.SenderId == userId && x.ReceiverId == myId))
       .OrderBy(x => x.MessageTime)
       .ToListAsync();

        var user = await _userManager.FindByIdAsync(userId);

        ViewBag.ReceiverId = userId;
        ViewBag.ReceiverName = user.UserName;
        ViewBag.Messages = messages;
        //return PartialView();
        return View();
    }
}