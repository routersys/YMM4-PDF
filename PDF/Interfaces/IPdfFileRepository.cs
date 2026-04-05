using System.Collections.Immutable;

namespace PDF.Interfaces
{
    public interface IPdfFileRepository
    {
        ImmutableArray<string> GetPdfFilesInDirectory(string directoryPath);
    }
}
