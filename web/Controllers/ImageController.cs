using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ropu.Web.Services;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class ImageController : Controller
    {
        readonly IImageService _imageService;

        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }
        
        [HttpGet("{hash}")]
        [AllowAnonymous]
        public ActionResult Image(string hash)
        {
            var imageBytes = _imageService.Get(hash);
            if(imageBytes == null)
            {
                return NotFound();
            }
            return new FileContentResult(imageBytes, "image/png");
        }

        [HttpGet("[action]")]
        [AllowAnonymous]
        public ActionResult Ropu()
        {
            var iconBytes = System.IO.File.ReadAllBytes("../Icon/Ropu.svg");
            return new FileContentResult(iconBytes, "image/svg+xml");
        }

        public class ImageResult
        {
            public string? Hash
            {
                get;
                set;
            }
        }

        [HttpPost("[action]")]
        public async Task<ActionResult<ImageResult>> Upload(IFormFile image)
        {
            using (var memoryStream = new MemoryStream())
            {
                await image.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                
                var hash = _imageService.Add(imageBytes);
                return new ImageResult{Hash = hash};
            }
        }
    }
}
