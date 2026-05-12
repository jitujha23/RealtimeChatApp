using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealtimeChatApp.Data;
using RealtimeChatApp.Models;
using System.Security.Claims;
namespace RealtimeChatApp.Controllers
{
    public class SwadeshichatController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hub;
        private readonly IWebHostEnvironment _env;

        public SwadeshichatController(
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
        public IActionResult Index()
        {
            var myId = _userManager.GetUserId(User);

            var users = _context.Users
                .Where(u => u.Id != myId)
                .ToList();

            //return View(users);
            //var users = _userManager.Users.ToList();   // ya jo tumhara user source hai
            return View(users);
        }
        [HttpGet]
        public async Task<IActionResult> GetMessagesold(string userId, int take = 50, long? beforeId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            IQueryable<ChatMessage> query = _context.ChatMessages
                .Where(x => (x.SenderId == userId && x.ReceiverId == currentUserId) ||
                            (x.SenderId == currentUserId && x.ReceiverId == userId))
                .OrderByDescending(x => x.Id);

            if (beforeId.HasValue)
            {
                query = query.Where(x => x.Id < beforeId.Value); // ✅ type-safe long comparison
            }

            var messages = await query.Take(take)
                                      .OrderBy(x => x.Id)
                                      .Select(x => new { x.Id, x.SenderId, x.Message, x.FilePath, x.IsSeen })
                                      .ToListAsync();

            return Json(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string userId, int take = 50, int? beforeId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            IQueryable<ChatMessage> query = _context.ChatMessages
                .Where(x => (x.SenderId == userId && x.ReceiverId == currentUserId) ||
                            (x.SenderId == currentUserId && x.ReceiverId == userId))
                .OrderByDescending(x => x.Id);

            if (beforeId.HasValue)
            {
                query = query.Where(x => x.Id < beforeId.Value);
            }

            var messages = await query
     .Take(take)
     .OrderBy(x => x.Id) // oldest first
     .Select(x => new
     {
         x.Id,
         x.SenderId,
         x.Message,
         x.FilePath,
         x.IsSeen
     })
     .ToListAsync();
            return Json(messages);
            //return Json(messages ?? new List<object>()); // always valid JSON
        }

    }
}
