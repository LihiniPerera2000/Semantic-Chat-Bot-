//using System.Text;
//using System.Text.RegularExpressions;

//namespace SemanticBotStar.Utils
//{
//    public class FileParser
//    {
//    }
//}

using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;


//using UglyToad.PdfPig;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SemanticBotStar.Utils
{
    public static class FileParser
    {
        public static string ParseFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".txt" => CleanText(File.ReadAllText(path)),
                ".md" => CleanText(File.ReadAllText(path)),
                ".docx" => CleanText(ParseDocx(path)),
                ".pdf" => CleanText(ParsePdf(path)),
                _ => throw new NotSupportedException($"Unsupported file type: {ext}")
            };
        }

        public static IEnumerable<string> ChunkText(string text, int chunkSize = 800, int overlap = 100)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            // Sentence-aware chunking
            var sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");
            var sb = new StringBuilder();

            foreach (var sentence in sentences)
            {
                if (sb.Length + sentence.Length > chunkSize)
                {
                    yield return sb.ToString().Trim();
                    // keep overlap for context continuity
                    sb.Clear();
                }
                sb.Append(sentence).Append(' ');
            }

            if (sb.Length > 0)
                yield return sb.ToString().Trim();
        }

        private static string ParseDocx(string path)
        {
            using var doc = WordprocessingDocument.Open(path, false);
            var body = doc.MainDocumentPart?.Document.Body;
            return body?.InnerText ?? string.Empty;
        }

        private static string ParsePdf(string path)
        {
            var sb = new StringBuilder();
            using var pdf = PdfDocument.Open(path);
            foreach (var page in pdf.GetPages())
            {
                // PdfPig's .Text sometimes misses spaces between words
                // Using Words allows us to insert proper spacing
                var words = page.GetWords();
                var line = string.Join(" ", words.Select(w => w.Text));
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        private static string CleanText(string text)
        {
            // Normalize spaces and remove excessive whitespace
            text = text.Replace("\r", " ").Replace("\n", " ");
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
    }
}
