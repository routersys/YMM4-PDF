using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ymm4Pdf.Shape;
using YukkuriMovieMaker.Commons;

namespace Ymm4Pdf.UI
{
    public partial class PdfFileSelector : UserControl, IPropertyEditorControl
    {
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(PdfFileSelector),
                new FrameworkPropertyMetadata(
                    "",
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        public ObservableCollection<PdfFileItem> PdfFiles { get; } = new ObservableCollection<PdfFileItem>();

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        private bool isUpdatingSelection = false;

        public PdfFileSelector()
        {
            InitializeComponent();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PdfFileSelector control)
            {
                control.UpdateSelectedFile();
            }
        }

        private void UpdateSelectedFile()
        {
            if (isUpdatingSelection) return;

            var selectedItem = PdfFiles.FirstOrDefault(f => f.FilePath.Equals(Value, StringComparison.OrdinalIgnoreCase));
            if (selectedItem != null)
            {
                if (PART_ComboBox.SelectedItem != selectedItem)
                {
                    PART_ComboBox.SelectedItem = selectedItem;
                }
            }
            else
            {
                UpdateFileList(true);
            }
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            UpdateFileList(false);
        }

        private void UpdateFileList(bool forceSelect)
        {
            var currentFilePath = Value;
            if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
            {
                PdfFiles.Clear();
                return;
            }

            var directory = Path.GetDirectoryName(currentFilePath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                PdfFiles.Clear();
                return;
            }

            try
            {
                var filesInDirectory = Directory.GetFiles(directory, "*.pdf")
                                                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                                .ToList();

                var currentFilePaths = PdfFiles.Select(f => f.FilePath).ToList();

                var filesToRemove = currentFilePaths.Except(filesInDirectory).ToList();
                foreach (var path in filesToRemove)
                {
                    var item = PdfFiles.FirstOrDefault(f => f.FilePath == path);
                    if (item != null) PdfFiles.Remove(item);
                }

                var filesToAdd = filesInDirectory.Except(currentFilePaths).ToList();
                foreach (var path in filesToAdd)
                {
                    var newItem = new PdfFileItem(path);
                    PdfFiles.Add(newItem);
                }

                foreach (var item in PdfFiles)
                {
                    _ = item.LoadThumbnailAsync();
                }

                if (forceSelect || PART_ComboBox.SelectedItem == null)
                {
                    var itemToSelect = PdfFiles.FirstOrDefault(f => f.FilePath.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase));
                    if (itemToSelect != null)
                    {
                        PART_ComboBox.SelectedItem = itemToSelect;
                    }
                }
            }
            catch
            {
                PdfFiles.Clear();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is PdfFileItem selectedItem)
            {
                isUpdatingSelection = true;
                if (Value != selectedItem.FilePath)
                {
                    BeginEdit?.Invoke(this, EventArgs.Empty);
                    Value = selectedItem.FilePath;
                    EndEdit?.Invoke(this, EventArgs.Empty);
                }
                isUpdatingSelection = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = Translate.PdfFileFilter,
                Title = Translate.SelectPdfFile
            };

            if (!string.IsNullOrEmpty(Value) && File.Exists(Value))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(Value);
            }

            if (dialog.ShowDialog() == true)
            {
                BeginEdit?.Invoke(this, EventArgs.Empty);
                Value = dialog.FileName;
                EndEdit?.Invoke(this, EventArgs.Empty);
                UpdateFileList(true);
            }
        }
    }
}