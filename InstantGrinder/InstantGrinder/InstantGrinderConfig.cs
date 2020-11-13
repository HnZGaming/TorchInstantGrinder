using System.Xml.Serialization;
using Torch;
using Torch.Views;

namespace InstantGrinder
{
    public sealed class InstantGrinderConfig : ViewModel
    {
        bool _enabled;

        [XmlElement("Enabled")]
        [Display(Order = 0, Name = "Enabled")]
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        // ReSharper disable once RedundantAssignment
        void SetProperty<T>(ref T property, T value)
        {
            property = value;
            OnPropertyChanged();
        }
    }
}