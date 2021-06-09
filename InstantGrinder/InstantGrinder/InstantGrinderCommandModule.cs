using NLog;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;

namespace InstantGrinder
{
    [Category("grind")]
    public sealed class InstantGrinderCommandModule : CommandModule
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        InstantGrinderPlugin Plugin => (InstantGrinderPlugin) Context.Plugin;
        InstantGrinderConfig Config => Plugin.Config;
        Core.InstantGrinder Grinder => Plugin.Grinder;

        [Command("configs", "List of configs.")]
        [Permission(MyPromoteLevel.None)]
        public void Configs()
        {
            this.GetOrSetProperty(Config);
        }

        [Command("commands", "List of commands.")]
        [Permission(MyPromoteLevel.None)]
        public void Commands()
        {
            this.ShowCommands();
        }

        [Command("name", " transfer components to character inventory.")]
        [Permission(MyPromoteLevel.None)]
        public void GrindByName(string gridName, bool force = false, bool asPlayer = false) => this.CatchAndReport(() =>
        {
            if (!Config.Enabled)
            {
                throw new UserFacingException("Plugin not active");
            }

            var player = Context.Player as MyPlayer;

            Grinder.GrindGridByName(player, gridName, force, asPlayer);
            Context.Respond($"Finished grinding grid: {gridName}");
        });
    }
}