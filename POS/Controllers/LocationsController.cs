using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Datas;
using POS.Models;

namespace POS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly WebSocketConnectionManager _webSocketConnectionManager;
        public LocationsController(AppDbContext context, WebSocketConnectionManager webSocketConnectionManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webSocketConnectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(webSocketConnectionManager));
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetLocations()
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var locations = _context.Locations.ToList();

            return Ok(locations);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateLocation([FromBody] Locations location)
        {
            if (location == null)
                return BadRequest("Location data is required.");

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "new-location",
                content = location
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return CreatedAtAction(nameof(GetLocations), new { id = location.LocationId }, location);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] Locations updatedLocation)
        {
            if (updatedLocation == null)
                return BadRequest("Location data is required.");

            var existingLocation = await _context.Locations.FindAsync(id);
            if (existingLocation == null)
                return NotFound($"Location with ID {id} not found.");

            existingLocation.Name = updatedLocation.Name;

            await _context.SaveChangesAsync();

            var message = new
            {
                type = "update-location",
                content = existingLocation
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return Ok(existingLocation);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return NotFound($"Location with ID {id} not found.");

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "delete-location",
                content = new { id = location.LocationId, name = location.Name }
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return NoContent();
        }

    }
}
