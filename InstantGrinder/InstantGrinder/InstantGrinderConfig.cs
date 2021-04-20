using System.Xml.Serialization;
using Torch;
using Torch.Views;

namespace InstantGrinder
{
    public sealed class InstantGrinderConfig : ViewModel
    {
        bool _enabled;

        [XmlElement]
        [Display(Order = 0, Name = "Enabled")]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }
    }
}