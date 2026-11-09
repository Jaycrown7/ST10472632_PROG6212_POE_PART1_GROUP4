using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/uploads")]
    [ApiController]
    [Authorize]
    public class UploadsController : ControllerBase
    {
        private readonly AzureBlobStorageService _storageService;

        public UploadsController(AzureBlobStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please select a file.");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File must be 5 MB or smaller.");

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Only JPG, PNG and WEBP images are allowed.");

            var imageUrl = await _storageService.UploadFileAsync(file);

            return Ok(new { imageUrl });
        }
    }
}