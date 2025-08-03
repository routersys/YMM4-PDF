using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Commons;

namespace ymm4_pdf
{
    public partial class PdfFileSelector : UserControl, IPropertyEditorControl
    {
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(PdfFileSelector), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public PdfFileSelector()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDFファイル (*.pdf)|*.pdf",
                Title = "PDFファイルを選択"
            };

            if (dialog.ShowDialog() == true)
            {
                BeginEdit?.Invoke(this, EventArgs.Empty);
                Value = dialog.FileName;
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}