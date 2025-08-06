using System.Windows;
using System.Windows.Data;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Views.Converters;

namespace Ymm4Pdf.UI
{
    internal class PdfFileSelectorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create()
        {
            return new PdfFileSelector();
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            var editor = (PdfFileSelector)control;
            editor.SetBinding(PdfFileSelector.ValueProperty, ItemPropertiesBinding.Create(itemProperties));
        }

        public override void ClearBindings(FrameworkElement control)
        {
            BindingOperations.ClearBinding(control, PdfFileSelector.ValueProperty);
        }
    }
}
