using PDF.Views;
using System.Windows;
using System.Windows.Data;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Views.Converters;

namespace PDF.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class PdfFileSelectorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create() => new PdfFileSelector();

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            var editor = (PdfFileSelector)control;
            editor.SetBinding(
                PdfFileSelector.ValueProperty,
                ItemPropertiesBinding.Create2(itemProperties));
        }

        public override void ClearBindings(FrameworkElement control)
            => BindingOperations.ClearBinding(control, PdfFileSelector.ValueProperty);
    }
}
