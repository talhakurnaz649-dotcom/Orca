using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orca.Models;

namespace Orca.Controllers
{
    public class HomeController : Controller
    {
        private readonly OrcaDbContext _context;

        public HomeController(OrcaDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var featuredGames = _context.Games
                .Include(g => g.Category)
                .Where(g => g.IsFeatured && g.IsActive)
                .ToList();

            var categories = _context.Categories.ToList();

            ViewBag.FeaturedGames = featuredGames;
            ViewBag.Categories = categories;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}