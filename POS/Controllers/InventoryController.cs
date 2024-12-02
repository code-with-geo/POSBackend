using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Datas;
using POS.Models;

namespace POS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly WebSocketConnectionManager _webSocketConnectionManager;

        public InventoryController(AppDbContext context, WebSocketConnectionManager webSocketConnectionManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webSocketConnectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(webSocketConnectionManager));
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetInventory()
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var inventory = _context.Inventory
                .Include(p => p.Products) 
                .Include(p => p.Locations)
                .ToList();

            return Ok(inventory);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateInventory([FromBody] Inventory inventory)
        {
            if (inventory == null)
                return BadRequest("Inventory data is required.");

            _context.Inventory.Add(inventory);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "new-inventory",
                content = inventory
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return CreatedAtAction(nameof(GetInventory), new { id = inventory.InventoryId }, inventory);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] Inventory updatedInventory)
        {
            if (updatedInventory == null)
                return BadRequest("Inventory data is required.");

            var existingInventory = await _context.Inventory.FindAsync(id);
            if (existingInventory == null)
                return NotFound($"Inventory with ID {id} not found.");

            // Update fields
            existingInventory.Units = updatedInventory.Units;
            existingInventory.Status = updatedInventory.Status;

            await _context.SaveChangesAsync();

            var message = new
            {
                type = "update-inventory",
                content = existingInventory
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return Ok(existingInventory);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory == null)
                return NotFound($"Inventory with ID {id} not found.");

            _context.Inventory.Remove(inventory);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "delete-product",
                content = new { id = inventory.InventoryId }
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return NoContent();
        }
    }
}
