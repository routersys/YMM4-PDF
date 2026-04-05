using PDF.Interfaces;
using System.Collections.Immutable;
using System.IO;

namespace PDF.Services
{
    public sealed class PdfFileRepository : IPdfFileRepository
    {
        public ImmutableArray<string> GetPdfFilesInDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return [];

            try
            {
                var files = Directory.GetFiles(directoryPath, "*.pdf");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                return [.. files];
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}
