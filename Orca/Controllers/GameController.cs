using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orca.Models;

namespace Orca.Controllers
{
    public class GameController : Controller
    {
        private readonly OrcaDbContext _context;

        public GameController(OrcaDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? categoryId, string? search)
        {
            var games = _context.Games
                .Include(g => g.Category)
                .Where(g => g.IsActive)
                .AsQueryable();

            if (categoryId.HasValue)
                games = games.Where(g => g.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(search))
                games = games.Where(g => g.Title.Contains(search));

            var categories = _context.Categories.ToList();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;

            return View(games.ToList());
        }

        public IActionResult Detail(int id)
        {
            var game = _context.Games
                .Include(g => g.Category)
                .FirstOrDefault(g => g.GameId == id);

            if (game == null)
                return NotFound();

            var similarGames = _context.Games
                .Include(g => g.Category)
                .Where(g => g.CategoryId == game.CategoryId && g.GameId != id && g.IsActive)
                .Take(4)
                .ToList();

            ViewBag.SimilarGames = similarGames;

            return View(game);
        }
    }
}