// AdminController.cs
using Microsoft.AspNetCore.Mvc;
using EmoTagger.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using EmoTagger.Data;

namespace EmoTagger.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalMusics = _context.Musics.Count();
            ViewBag.TotalTags = _context.MusicTags.Count();
            ViewBag.RecentUsers = _context.Users.OrderByDescending(u => u.CreatedAt).Take(5).ToList();
            ViewBag.RecentMusics = _context.Musics.OrderByDescending(m => m.createdat).Take(5).ToList();
            return View();
        }

        public IActionResult Users(string search)
        {
            var users = string.IsNullOrWhiteSpace(search)
                ? _context.Users.ToList()
                : _context.Users.Where(u =>
                    u.FirstName.ToLower().Contains(search.ToLower()) ||
                    u.LastName.ToLower().Contains(search.ToLower()) ||
                    u.Email.ToLower().Contains(search.ToLower())
                ).ToList();

            ViewBag.Search = search;
            return View(users);
        }


        public IActionResult Musics(string search)
        {
            var musics = string.IsNullOrWhiteSpace(search)
                ? _context.Musics.ToList()
                : _context.Musics.Where(m =>
                    m.title.ToLower().Contains(search.ToLower()) ||
                    m.artist.ToLower().Contains(search.ToLower())
                ).ToList();

            var musicTags = _context.MusicTags.Include(mt => mt.User).Include(mt => mt.Music).ToList();

            var tagCounts = musicTags
                .GroupBy(mt => mt.MusicId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.MusicTags = musicTags;
            ViewBag.TagCounts = tagCounts;
            ViewBag.Search = search;

            return View(musics);
        }


        [HttpPost]
        public IActionResult AddMusic(string title, string artist)
        {
            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
            {
                _context.Musics.Add(new Music { title = title, artist = artist, createdat = DateTime.UtcNow });
                _context.SaveChanges();
            }
            return RedirectToAction("Musics");
        }

        [HttpPost]
        public IActionResult DeleteMusic(int id)
        {
            var music = _context.Musics.Find(id);
            if (music != null)
            {
                _context.Musics.Remove(music);
                _context.SaveChanges();
            }
            return RedirectToAction("Musics");
        }
    }
}
