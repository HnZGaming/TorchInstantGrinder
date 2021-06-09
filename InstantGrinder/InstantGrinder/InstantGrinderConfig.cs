using System.Xml.Serialization;
using Torch;
using Torch.Views;

namespace InstantGrinder
{
    public sealed class InstantGrinderConfig : ViewModel, Core.InstantGrinder.IConfig
    {
        bool _enabled = true;
        double _distanceThreshold = 100;

        [XmlElement]
        [Display(Order = 0, Name = "Enabled")]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        [XmlElement]
        [Display(Order = 1, Name = "Distance Threshold")]
        public double MaxDistance
        {
            get => _distanceThreshold;
            set => SetValue(ref _distanceThreshold, value);
        }
    }
}