//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using SemanticBotStar.Services;

//namespace SemanticBotStar.Pages
//{
//    public class UploadModel : PageModel
//    {
//        public void OnGet()
//        {
//        }
//    }
//}


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace SemanticBotStar.Pages
{
    public class UploadModel : PageModel
    {
        [BindProperty]
        public IFormFileCollection UploadedFiles { get; set; }

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public string Category { get; set; }

        [BindProperty]
        public string Tags { get; set; }

        [BindProperty]
        public string Description { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFiles == null || UploadedFiles.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a PDF file first.");
                return Page();
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string savedFileName = null;

            foreach (var file in UploadedFiles)
            {
                if (file.ContentType != "application/pdf") continue;
                savedFileName = Path.Combine(uploadPath, Path.GetFileName(file.FileName));
                using (var stream = new FileStream(savedFileName, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            // Save metadata as JSON
            var metadata = new
            {
                Title,
                Category,
                Tags,
                Description,
                UploadedFile = Path.GetFileName(savedFileName),
                UploadedOn = DateTime.Now
            };

            var metadataPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "metadata.json");

            var existingList = new List<object>();
            if (System.IO.File.Exists(metadataPath))
            {
                var existing = await System.IO.File.ReadAllTextAsync(metadataPath);
                existingList = JsonSerializer.Deserialize<List<object>>(existing) ?? new List<object>();
            }

            existingList.Add(metadata);
            await System.IO.File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(existingList, new JsonSerializerOptions { WriteIndented = true }));

            TempData["Message"] = "File and metadata saved successfully!";
            return RedirectToPage();
        }
    }
}
