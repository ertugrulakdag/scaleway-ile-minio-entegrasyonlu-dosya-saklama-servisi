using FileWebApiDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileWebApiDemo.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : Controller
    {
        private readonly MinioService _minioService;

        public FileController(MinioService minioService)
        {
            _minioService = minioService;
        }

        [HttpPost("upload/{folderName}")]
        public async Task<IActionResult> Upload(string folderName, [FromForm] IFormFile file)
        {
            await _minioService.UploadFileAsync(file, folderName);
            return Ok("Dosya yüklendi.");
        }

        [HttpGet("download/{folderName}/{fileName}")]
        public async Task<IActionResult> Download(string folderName, string fileName)
        {
            var fileStream = await _minioService.GetFileAsync(fileName, folderName);
            return File(fileStream, "application/octet-stream", fileName);
        }
    }
}
