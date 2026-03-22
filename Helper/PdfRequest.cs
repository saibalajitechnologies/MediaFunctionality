namespace FunctionalitiesWebAPI.Helper
{
    public class PdfRequest
    {
        public List<IFormFile> Images { get; set; } = new();
        public string OutputFileName { get; set; } = "output.pdf";
    }
}
