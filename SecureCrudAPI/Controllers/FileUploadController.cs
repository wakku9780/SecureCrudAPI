using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureCrudAPI.Services;

namespace SecureCrudAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly FileUploadService _fileUploadService;

        public FileUploadController(FileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                var fileUrl = await _fileUploadService.UploadFileAsync(file);
                return Ok(new { fileUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
