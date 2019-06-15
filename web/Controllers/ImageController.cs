using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ropu.Web.Models;
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
    }
}
