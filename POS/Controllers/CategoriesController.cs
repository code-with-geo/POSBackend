using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Datas;
using POS.Models;

namespace POS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly WebSocketConnectionManager _webSocketConnectionManager;

        public CategoriesController(AppDbContext context, WebSocketConnectionManager webSocketConnectionManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webSocketConnectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(webSocketConnectionManager));
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetCategories()
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var categories = _context.Categories.ToList();

            return Ok(categories);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCategory([FromBody] Categories category)
        {
            if (category == null)
                return BadRequest("Category data is required.");

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "new-category",
                content = category
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return CreatedAtAction(nameof(GetCategories), new { id = category.CategoryId }, category);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Categories updatedCategory)
        {
            if (updatedCategory == null)
                return BadRequest("Category data is required.");

            var existingCategory = await _context.Categories.FindAsync(id);
            if (existingCategory == null)
                return NotFound($"Category with ID {id} not found.");

            existingCategory.Name = updatedCategory.Name;

            await _context.SaveChangesAsync();

            var message = new
            {
                type = "update-category",
                content = existingCategory
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return Ok(existingCategory);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound($"Category with ID {id} not found.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "delete-category",
                content = new { id = category.CategoryId, name = category.Name }
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return NoContent();
        }
    }
}
