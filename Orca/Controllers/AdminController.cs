using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orca.Models;

namespace Orca.Controllers
{
    public class AdminController : Controller
    {
        private readonly OrcaDbContext _context;

        public AdminController(OrcaDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("IsAdmin") == "true";

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.TotalGames = _context.Games.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalRevenue = _context.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.RecentOrders = _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            ViewBag.RecentUsers = _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToList();

            return View();
        }

        public IActionResult Games()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var games = _context.Games.Include(g => g.Category).ToList();
            ViewBag.Categories = _context.Categories.ToList();
            return View(games);
        }

        public IActionResult CreateGame()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult CreateGame(string title, string description, decimal price, decimal? oldPrice, string imageUrl, int categoryId, string platform, decimal rating, int stock, bool isActive, bool isFeatured)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var game = new Game
            {
                Title = title,
                Description = description,
                Price = price,
                OldPrice = oldPrice,
                ImageUrl = imageUrl,
                CategoryId = categoryId,
                Platform = platform,
                Rating = rating,
                Stock = stock,
                IsActive = isActive,
                IsFeatured = isFeatured,
                CreatedAt = DateTime.Now
            };

            _context.Games.Add(game);
            _context.SaveChanges();

            return RedirectToAction("Games");
        }

        public IActionResult EditGame(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var game = _context.Games.FirstOrDefault(g => g.GameId == id);
            if (game == null) return NotFound();
            ViewBag.Categories = _context.Categories.ToList();
            return View(game);
        }

        [HttpPost]
        public IActionResult EditGame(int gameId, string title, string description, decimal price, decimal? oldPrice, string imageUrl, int categoryId, string platform, decimal rating, int stock, bool isActive, bool isFeatured)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var game = _context.Games.FirstOrDefault(g => g.GameId == gameId);
            if (game == null) return NotFound();

            game.Title = title;
            game.Description = description;
            game.Price = price;
            game.OldPrice = oldPrice;
            game.ImageUrl = imageUrl;
            game.CategoryId = categoryId;
            game.Platform = platform;
            game.Rating = rating;
            game.Stock = stock;
            game.IsActive = isActive;
            game.IsFeatured = isFeatured;

            _context.SaveChanges();
            return RedirectToAction("Games");
        }

        public IActionResult DeleteGame(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var game = _context.Games.FirstOrDefault(g => g.GameId == id);
            if (game != null)
            {
                _context.Games.Remove(game);
                _context.SaveChanges();
            }
            return RedirectToAction("Games");
        }

        public IActionResult Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var users = _context.Users.ToList();
            return View(users);
        }

        public IActionResult ToggleAdmin(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                user.IsAdmin = !user.IsAdmin;
                _context.SaveChanges();
            }
            return RedirectToAction("Users");
        }

        public IActionResult Orders()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Game)
                .ToList();
            return View(orders);
        }
    }
}