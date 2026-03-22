//using FunctionalitiesWebAPI.Helper;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace FunctionalitiesWebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class PdfController : ControllerBase
//    {
//        private readonly PdfService _pdfService;
//        private readonly IWebHostEnvironment _env;

//        public PdfController(PdfService pdfService, IWebHostEnvironment env)
//        {
//            _pdfService = pdfService;
//            _env = env;
//        }

//        [HttpPost("generateImageFromPDF")]
//        public IActionResult GeneratePdf([FromForm] IFormFile file, [FromForm] string outputFileName)
//        {
//            try
//            {
//                if (file == null || file.Length == 0)
//                    return BadRequest("No file uploaded.");

//                string tempFolder = Path.Combine(_env.ContentRootPath, "TempFiles", Guid.NewGuid().ToString());
//                Directory.CreateDirectory(tempFolder);

//                string uploadedFile = Path.Combine(tempFolder, file.FileName);
//                using (var fs = new FileStream(uploadedFile, FileMode.Create))
//                {
//                    file.CopyTo(fs);
//                }

//                var imageFolder = Path.Combine(tempFolder, "Images");
//                Directory.CreateDirectory(imageFolder);

//                var images = new List<string>();

//                if (file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
//                {
//                    var splitFolder = Path.Combine(tempFolder, "Split");
//                    _pdfService.SplitPdf(uploadedFile, splitFolder);

//                    foreach (var pagePdf in Directory.GetFiles(splitFolder, "*.pdf"))
//                    {
//                        _pdfService.ConvertPdfToImages(pagePdf, imageFolder);
//                    }
//                }
//                else if (file.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
//                {
//                    images.Add(uploadedFile);
//                }

//                images.AddRange(Directory.GetFiles(imageFolder, "*.png"));

//                string outputPdf = Path.Combine(_env.ContentRootPath, $"{outputFileName}.pdf");
//                _pdfService.ConvertImagesToPdf(images.ToList(), outputPdf);

//                var fileBytes = System.IO.File.ReadAllBytes(outputPdf);
//                return File(fileBytes, "application/pdf", $"{outputFileName}.pdf");
//            }
//            catch (Exception ex)
//            {
//                // Log the exception (e.g., Console or ILogger)
//                Console.WriteLine(ex);
//                return StatusCode(500, ex.Message);
//            }
//        }
//    }
//}