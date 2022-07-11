using System.Windows.Controls;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Utils.Torch;

namespace InstantGrinder
{
    public sealed class InstantGrinderPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        Persistent<InstantGrinderConfig> _config;
        UserControl _userControl;
        Core.InstantGrinder _grinder;

        public InstantGrinderConfig Config => _config.Data;
        public Core.InstantGrinder Grinder => _grinder;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var configFilePath = this.MakeConfigFilePath();
            _config = Persistent<InstantGrinderConfig>.Load(configFilePath);

            _grinder = new Core.InstantGrinder(Config);
        }

        public UserControl GetControl()
        {
            return _config.GetOrCreateUserControl(ref _userControl);
        }
    }
}