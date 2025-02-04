using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureCrudAPI.Models;
using SecureCrudAPI.Services;

namespace SecureCrudAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly FileUploadService _fileUploadService;

        public ProductController(UserDbContext context, FileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        // Get All Products
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        // Get Product by Id
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        /// <summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromForm] Product product, IFormFile file)
        {
            if (file != null)
            {
                var imageUrl = await _fileUploadService.UploadFileAsync(file);  // Upload file to Cloudinary
                product.ImageUrl = imageUrl;  // Set the image URL in Product
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product created successfully!" });
        }



        /// <returns></returns>
        //// Create Product (Admin Only)
        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> CreateProduct([FromBody] Product product)
        //{
        //    _context.Products.Add(product);
        //    await _context.SaveChangesAsync();
        //    return Ok(new { Message = "Product created successfully!" });
        //}



        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] Product product, IFormFile file)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;

            if (file != null)
            {
                var imageUrl = await _fileUploadService.UploadFileAsync(file);  // Upload file to Cloudinary
                existingProduct.ImageUrl = imageUrl;  // Update the image URL
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product updated successfully!" });
        }





        // Update Product (Admin Only)
        //[HttpPut("{id}")]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        //{
        //    var existingProduct = await _context.Products.FindAsync(id);
        //    if (existingProduct == null)
        //    {
        //        return NotFound();
        //    }

        //    existingProduct.Name = product.Name;
        //    existingProduct.Description = product.Description;
        //    existingProduct.Price = product.Price;

        //    await _context.SaveChangesAsync();
        //    return Ok(new { Message = "Product updated successfully!" });
        //}

        // Delete Product (Admin Only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product deleted successfully!" });
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredProducts(
                                    [FromQuery] decimal? minPrice,
                                    [FromQuery] decimal? maxPrice,
                                    [FromQuery] string? category,
                                    [FromQuery] string? search
            )
        {
            var query = _context.Products.AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            var products = await query.ToListAsync();
            return Ok(products);
        }



        [HttpGet("paginated")]
        public async Task<IActionResult> GetPaginatedProducts(
      [FromQuery] int pageNumber = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] string? sortBy = "Name", // Default sorting by Name
      [FromQuery] string? sortOrder = "asc") // Default ascending
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest(new { Message = "Invalid page number or page size" });

            var query = _context.Products.AsQueryable();

            // Sorting Logic
            query = sortOrder.ToLower() == "desc"
                ? query.OrderByDescending(p => EF.Property<object>(p, sortBy))
                : query.OrderBy(p => EF.Property<object>(p, sortBy));

            // Pagination Logic
            var totalRecords = await query.CountAsync();
            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Products = products
            });
        }









    }

}
