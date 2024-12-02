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

        public ProductController(AppDbContext context, WebSocketConnectionManager webSocketConnectionManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webSocketConnectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(webSocketConnectionManager));
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetProducts()
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var products = _context.Products
                .Include(p => p.Categories) // Eager load related categories
                .ToList();

            return Ok(products);
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetProductById(int id)
        {
            if (_context == null)
                return StatusCode(500, "Database context is null.");

            var product = _context.Products
                .Include(p => p.Categories) // Eager load related categories
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound($"Product with ID {id} not found.");

            return Ok(product);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromBody] Products product)
        {
            if (product == null)
                return BadRequest("Product data is required.");

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "new-product",
                content = product
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Products updatedProduct)
        {
            if (updatedProduct == null)
                return BadRequest("Product data is required.");

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
                return NotFound($"Product with ID {id} not found.");

            // Update fields
            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;
            existingProduct.CategoryId = updatedProduct.CategoryId;
            existingProduct.Status = updatedProduct.Status;

            await _context.SaveChangesAsync();

            var message = new
            {
                type = "update-product",
                content = existingProduct
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return Ok(existingProduct);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound($"Product with ID {id} not found.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            var message = new
            {
                type = "delete-product",
                content = new { id = product.Id, name = product.Name }
            };

            await _webSocketConnectionManager.BroadcastJsonMessageAsync(message);

            return NoContent();
        }
    }
}
