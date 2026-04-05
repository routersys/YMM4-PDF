using PDF.Models;

namespace PDF.Interfaces
{
    public interface IPdfRenderService
    {
        RenderResult RenderPage(string filePath, int pageIndex, float scale);
        RenderResult RenderThumbnail(string filePath, float scale = 0.15f);
    }
}
