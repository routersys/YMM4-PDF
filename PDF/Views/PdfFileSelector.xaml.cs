using Microsoft.Win32;
using PDF.Interfaces;
using PDF.Localization;
using PDF.Services;
using PDF.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Commons;

namespace PDF.Views
{
    public partial class PdfFileSelector : UserControl, IPropertyEditorControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(PdfFileSelector),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        private readonly PdfFileSelectorViewModel _viewModel;
        private bool _suppressSync;

        public PdfFileSelector()
        {
            _viewModel = new PdfFileSelectorViewModel(
                ServiceLocator.Instance.Resolve<IPdfRenderService>(),
                ServiceLocator.Instance.Resolve<IPdfFileRepository>());

            _viewModel.FileSelected += OnViewModelFileSelected;
            DataContext = _viewModel;
            InitializeComponent();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PdfFileSelector control && !control._suppressSync)
                control._viewModel.SyncExternalPath((string)e.NewValue);
        }

        private void OnViewModelFileSelected(object? sender, string filePath)
        {
            if (string.Equals(Value, filePath, StringComparison.OrdinalIgnoreCase)) return;

            _suppressSync = true;
            BeginEdit?.Invoke(this, EventArgs.Empty);
            Value = filePath;
            EndEdit?.Invoke(this, EventArgs.Empty);
            _suppressSync = false;
        }

        private void OnDropDownOpened(object sender, EventArgs e)
            => _viewModel.RefreshFileList();

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = Texts.PdfFileFilter,
                Title = Texts.SelectPdfFile,
            };

            if (!string.IsNullOrEmpty(Value) && File.Exists(Value))
                dialog.InitialDirectory = Path.GetDirectoryName(Value);

            if (dialog.ShowDialog() != true) return;

            BeginEdit?.Invoke(this, EventArgs.Empty);
            Value = dialog.FileName;
            EndEdit?.Invoke(this, EventArgs.Empty);

            _viewModel.SyncExternalPath(dialog.FileName);
            _viewModel.RefreshFileList(selectCurrentFile: true);
        }
    }
}
