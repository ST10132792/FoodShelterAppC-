using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodShelterAppC_.Data;
using FoodShelterAppC_.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using FoodShelterAppC_.Services;

namespace FoodShelterAppC_.Controllers
{
    [Authorize]
    public class ShelterLocationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly OpenStreetMapService _geocodingService;

        public ShelterLocationController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            OpenStreetMapService geocodingService)
        {
            _context = context;
            _userManager = userManager;
            _geocodingService = geocodingService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ShelterLocation shelterLocation)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var (latitude, longitude) = await _geocodingService.GeocodeAddress(shelterLocation.Address);
                
                if (latitude.HasValue && longitude.HasValue)
                {
                    shelterLocation.Latitude = latitude.Value;
                    shelterLocation.Longitude = longitude.Value;
                }

                var user = await _userManager.GetUserAsync(User);
                shelterLocation.UserId = user.Id;

                _context.ShelterLocations.Add(shelterLocation);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _context.ShelterLocations.FindAsync(id);
            if (location != null && location.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                _context.ShelterLocations.Remove(location);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Dashboard");
        }
    }
}