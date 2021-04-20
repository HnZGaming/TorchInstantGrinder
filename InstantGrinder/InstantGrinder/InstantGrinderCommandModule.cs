using NLog;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
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

        [Command("name", "Grind a grid by name.")]
        [Permission(MyPromoteLevel.None)]
        public void GrindByName(string gridName, bool force = false, bool asPlayer = false) => this.CatchAndReport(() =>
        {
            Grinder.GrindGridByName(Context.Player as MyPlayer, gridName, force, asPlayer);
            Context.Respond($"Finished grinding grid: {gridName}");
        });

        [Command("this", "Grind a grid that the player is looking at or seated on.")]
        [Permission(MyPromoteLevel.None)]
        public void GrindThis(bool force = false, bool asPlayer = false) => this.CatchAndReport(() =>
        {
            Grinder.GrindGridSelected(Context.Player as MyPlayer, force, asPlayer);
            Context.Respond("Finished grinding grid");
        });
    }
}