using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orca.Models;
namespace Orca.Controllers
{
    public class AccountController : Controller
    {
        private readonly OrcaDbContext _context;

        public AccountController(OrcaDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var hash = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == hash);

            if (user == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin ? "true" : "false");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string email, string password)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Bu kullanıcı adı zaten kullanılıyor.";
                return View();
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Bu e-posta zaten kullanılıyor.";
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.Now,
                IsAdmin = false
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("IsAdmin", "false");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
         public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
            if (user == null) return RedirectToAction("Login");

            var orders = _context.Orders
                .Where(o => o.UserId == userId.Value)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Game)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            ViewBag.Orders = orders;
            return View(user);
        }
    }
    }
