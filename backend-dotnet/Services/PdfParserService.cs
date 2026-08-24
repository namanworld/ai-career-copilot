using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace AiCareerCopilot.Api.Services;

public interface IPdfParserService
{
    string ExtractText(Stream pdfStream);
    string SanitizeText(string text);
}

public class PdfParserService : IPdfParserService
{
    private static readonly string[] InjectionPatterns = new[]
    {
        @"ignore\s+(all\s+)?(previous|prior)\s+instructions",
        @"disregard\s+(all\s+)?(previous|prior)\s+instructions",
        @"system\s+prompt\s+override",
        @"you\s+are\s+now\s+in\s+developer\s+mode",
        @"reveal\s+(system\s+)?(prompt|secret|key)",
        @"output\s+raw\s+system\s+prompt"
    };

    public string SanitizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Remove non-printable control characters
        string cleaned = Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);

        // Neutralize prompt injection attempts
        foreach (var pattern in InjectionPatterns)
        {
            cleaned = Regex.Replace(cleaned, pattern, "[FLAGGED_INJECTION_REMOVED]", RegexOptions.IgnoreCase);
        }

        return cleaned.Trim();
    }

    public string ExtractText(Stream pdfStream)
    {
        try
        {
            var sb = new StringBuilder();
            using var document = PdfDocument.Open(pdfStream);

            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }

            var fullText = sb.ToString();
            if (string.IsNullOrWhiteSpace(fullText))
            {
                throw new BadHttpRequestException("Could not extract readable text from PDF. Ensure the PDF is not an un-OCR'd scanned image.");
            }

            return SanitizeText(fullText);
        }
        catch (BadHttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BadHttpRequestException($"PDF parsing error: {ex.Message}");
        }
    }
}
