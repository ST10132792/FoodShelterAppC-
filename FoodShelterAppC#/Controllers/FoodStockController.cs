using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodShelterAppC_.Data;
using FoodShelterAppC_.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodShelterAppC_.Controllers
{
    [Authorize]
    public class FoodStockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FoodStockController> _logger;
        private readonly UserManager<User> _userManager;

        public FoodStockController(
            ApplicationDbContext context,
            ILogger<FoodStockController> logger,
            UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create(FoodStock foodStock)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state: {@ModelErrors}",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                foodStock.UserId = user.Id;
                foodStock.User = user;

                _context.FoodStocks.Add(foodStock);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Food stock item added successfully!";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating food stock item: {@FoodStock}", foodStock);
                TempData["Error"] = "Error adding food stock item. Please try again.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var foodStock = await _context.FoodStocks
                    .Include(f => f.MealIngredients)
                    .ThenInclude(mi => mi.MealPlan)
                    .FirstOrDefaultAsync(f => f.Id == id && f.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (foodStock == null)
                {
                    TempData["Error"] = "Food stock item not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                // Check if the food stock is used in any meal plans
                if (foodStock.MealIngredients.Any())
                {
                    var mealNames = foodStock.MealIngredients
                        .Select(mi => mi.MealPlan.Name)
                        .Distinct()
                        .ToList();

                    TempData["Error"] = $"Cannot delete {foodStock.ItemName} as it is used in the following meal plans: {string.Join(", ", mealNames)}. Please delete these meal plans first.";
                    return RedirectToAction("Index", "Dashboard");
                }

                _context.FoodStocks.Remove(foodStock);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Food stock item deleted successfully.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the food stock item.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, string> updates)
        {
            try
            {
                var foodStock = await _context.FoodStocks
                    .FirstOrDefaultAsync(f => f.Id == id && f.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (foodStock == null)
                {
                    return Json(new { success = false, message = "Food stock not found." });
                }

                if (updates.TryGetValue("Quantity", out string quantityStr))
                {
                    if (decimal.TryParse(quantityStr, out decimal quantity))
                    {
                        foodStock.Quantity = quantity;
                    }
                }
                foodStock.ItemName = updates["ItemName"];
                foodStock.Category = updates["Category"];
                foodStock.ExpirationDate = DateTime.TryParse(updates["ExpirationDate"]?.ToString(), out DateTime parsedDate)
    ? parsedDate
    : (DateTime?)null;

                foodStock.MinimumStock = (int)decimal.Parse(updates["MinimumStock"]);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating food stock." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItem(int id)
        {
            var foodStock = await _context.FoodStocks
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (foodStock == null)
            {
                return Json(new { success = false, message = "Food stock not found." });
            }

            return Json(new { 
                success = true, 
                item = new { 
                    itemName = foodStock.ItemName,
                    category = foodStock.Category,
                    quantity = foodStock.Quantity,
                    unit = foodStock.Unit,
                    minimumStock = foodStock.MinimumStock,
                    expirationDate = foodStock.ExpirationDate
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var foodStock = await _context.FoodStocks
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (foodStock == null)
            {
                TempData["Error"] = "Food stock item not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View(foodStock);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FoodStock model)
        {
            if (id != model.Id)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var foodStock = await _context.FoodStocks
                        .FirstOrDefaultAsync(f => f.Id == id && f.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

                    if (foodStock == null)
                    {
                        TempData["Error"] = "Food stock item not found.";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    foodStock.ItemName = model.ItemName;
                    foodStock.Category = model.Category;
                    foodStock.Quantity = model.Quantity;
                    foodStock.Unit = model.Unit;
                    foodStock.ExpirationDate = model.ExpirationDate;
                    foodStock.MinimumStock = model.MinimumStock;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Food stock updated successfully.";
                    return RedirectToAction("Index", "Dashboard");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error updating food stock.";
                }
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
