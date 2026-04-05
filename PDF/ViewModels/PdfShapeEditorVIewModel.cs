using PDF.Models;
using System.ComponentModel;

namespace PDF.ViewModels
{
    public sealed class PdfShapeEditorViewModel : INotifyPropertyChanged
    {
        private PdfShapeParameter[] _parameters = [];
        private object? _selectedTarget;

        public object? SelectedTarget
        {
            get => _selectedTarget;
            private set
            {
                if (!ReferenceEquals(_selectedTarget, value))
                {
                    _selectedTarget = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTarget)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        internal void SetParameters(PdfShapeParameter[] parameters)
        {
            foreach (var p in _parameters)
            {
                if (p is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= OnParameterPropertyChanged;
            }

            _parameters = parameters ?? [];

            foreach (var p in _parameters)
            {
                if (p is INotifyPropertyChanged npc)
                    npc.PropertyChanged += OnParameterPropertyChanged;
            }

            UpdateTargets();
        }

        private void OnParameterPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PdfShapeParameter.RenderMode))
                UpdateTargets();
        }

        private void UpdateTargets()
        {
            if (_parameters.Length == 0)
            {
                SelectedTarget = null;
                return;
            }

            var firstMode = _parameters[0].RenderMode;
            bool isUniformMode = true;
            for (int i = 1; i < _parameters.Length; i++)
            {
                if (_parameters[i].RenderMode != firstMode)
                {
                    isUniformMode = false;
                    break;
                }
            }

            if (!isUniformMode)
            {
                SelectedTarget = null;
                return;
            }

            if (firstMode == RenderMode.Vector)
            {
                SelectedTarget = new PdfShapeParameter.VectorParameters(_parameters[0]);
            }
            else
            {
                SelectedTarget = new PdfShapeParameter.RasterParameters(_parameters[0]);
            }
        }
    }
}
