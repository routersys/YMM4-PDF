using PDF.ViewModels;
using System.Windows.Controls;
using YukkuriMovieMaker.Commons;

namespace PDF.Views
{
    public partial class PdfShapeEditorControl : UserControl, IPropertyEditorControl
    {
        private readonly PdfShapeEditorViewModel _viewModel;

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public PdfShapeEditorControl()
        {
            InitializeComponent();
            _viewModel = new PdfShapeEditorViewModel();
            DataContext = _viewModel;
        }

        internal void SetParameters(PdfShapeParameter[] parameters)
        {
            _viewModel.SetParameters(parameters);
        }

        private void PropertiesEditor_BeginEdit(object sender, EventArgs e)
            => BeginEdit?.Invoke(this, EventArgs.Empty);

        private void PropertiesEditor_EndEdit(object sender, EventArgs e)
            => EndEdit?.Invoke(this, EventArgs.Empty);
    }
}
