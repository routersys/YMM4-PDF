using PDF.Localization;
using System.ComponentModel.DataAnnotations;

namespace PDF.Models
{
    public enum RenderMode
    {
        [Display(Name = nameof(Texts.Vector), ResourceType = typeof(Texts))]
        Vector,

        [Display(Name = nameof(Texts.Raster), ResourceType = typeof(Texts))]
        Raster,
    }
}
