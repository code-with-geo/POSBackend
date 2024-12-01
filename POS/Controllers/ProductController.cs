using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Datas;
using POS.Models;

namespace POS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly WebSocketConnectionManager _webSocketConnectionManager;

        // Constructor injection for AppDbContext and WebSocketConnectionManager
        public ProductController(AppDbContext context, WebSocketConnectionManager webSocketConnectionManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webSocketConnectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(webSocketConnectionManager));
        }

        // GET: api/product
        [HttpGet]
        [Authorize]
        public IActionResult GetProducts()
        {
            if (_context == null)
            {
                return StatusCode(500, "Database context is null.");
            }

            var products = _context.Products.ToList();
            return Ok(products);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromBody] Products product)
        {
            if (product == null)
            {
                return BadRequest("Product data is required.");
            }

            if (_context == null)
            {
                return StatusCode(500, "Database context is null.");
            }

            _context.Products.Add(product);
            _context.SaveChanges();

            // Broadcast a message to all connected WebSocket clients about the new product
            var message = new
            {
                type = "new-product",
                content = new
                {
                    id = product.Id,
                    name = product.Name,
                    price = product.Price,
                    units = product.Units
                }
            };

            // Wait for the broadcast to finish before returning the response
            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Products updatedProduct)
        {
            if (updatedProduct == null)
            {
                return BadRequest("Product data is required.");
            }

            var existingProduct = _context.Products.Find(id);
            if (existingProduct == null)
            {
                return NotFound($"Product with ID {id} not found.");
            }

            // Update fields of the existing product
            existingProduct.Name = updatedProduct.Name;
            existingProduct.Price = updatedProduct.Price;
            existingProduct.Units = updatedProduct.Units;

            _context.Products.Update(existingProduct);
            _context.SaveChanges();

            // Broadcast a message to all connected WebSocket clients about the updated product
            var message = new
            {
                type = "update-product",
                content = new
                {
                    id = existingProduct.Id,
                    name = existingProduct.Name,
                    price = existingProduct.Price,
                    units = existingProduct.Units
                }
            };

            // Wait for the broadcast to finish before returning the response
            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return Ok(existingProduct);  // Return the updated product
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound($"Product with ID {id} not found.");
            }

            _context.Products.Remove(product);
            _context.SaveChanges();

            // Broadcast a message to all connected WebSocket clients about the deleted product
            var message = new
            {
                type = "delete-product",
                content = new
                {
                    id = product.Id,
                    name = product.Name
                }
            };

            // Wait for the broadcast to finish before returning the response
            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return NoContent();  // Return 204 No Content for successful deletion
        }
    }
}
