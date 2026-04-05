using PDF.Views;
using System.Windows;
using YukkuriMovieMaker.Commons;

namespace PDF.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class PdfShapeEditorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create() => new PdfShapeEditorControl();

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            if (control is PdfShapeEditorControl editor && itemProperties.Length > 0)
            {
                var parameters = itemProperties
                    .Select(p => p.PropertyOwner as PdfShapeParameter)
                    .Where(p => p is not null)
                    .Cast<PdfShapeParameter>()
                    .ToArray();
                editor.SetParameters(parameters);
            }
        }

        public override void ClearBindings(FrameworkElement control)
        {
            if (control is PdfShapeEditorControl editor)
                editor.SetParameters([]);
        }
    }
}
