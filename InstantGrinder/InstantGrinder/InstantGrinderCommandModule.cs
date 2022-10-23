using System;
using System.Collections.Generic;
using NLog;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.Torch;
using VRage.Game.ModAPI;
using VRageMath;

namespace InstantGrinder
{
    [Category("grind")]
    public sealed class InstantGrinderCommandModule : CommandModule
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly Dictionary<ulong, (string Query, DateTime Timestamp)> _queries = new();

        InstantGrinderPlugin Plugin => (InstantGrinderPlugin)Context.Plugin;
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
        public void GrindByName(string gridName, bool asPlayer = false) => this.CatchAndReport(() =>
        {
            var player = Context.Player as MyPlayer;
            var pid = player?.SteamId() ?? 0L;
            var force = TestForce(pid, gridName);

            if (!Grinder.TryGrindByName(player, gridName, force, asPlayer, out var objection))
            {
                Context.Respond($"Failed grinding: \"{objection.Message}\"; To proceed, type the same command.", Color.Yellow);
                QueryForce(gridName, pid);
                return;
            }

            Context.Respond($"Finished grinding grid: {gridName}");
        });

        [Command("this", "Grind a grid that the player is looking at or seated on.")]
        [Permission(MyPromoteLevel.None)]
        public void GrindThis(bool asPlayer = false) => this.CatchAndReport(() =>
        {
            var player = Context.Player as MyPlayer;
            var pid = player?.SteamId() ?? 0L;
            var force = TestForce(pid, "this");

            if (!Grinder.GrindGridSelected(Context.Player as MyPlayer, force, asPlayer, out var objection))
            {
                Context.Respond($"Failed grinding: \"{objection.Message}\"; To proceed, type the same command.", Color.Yellow);
                QueryForce("this", pid);
                return;
            }

            Context.Respond("Finished grinding grid");
        });

        static bool TestForce(ulong pid, string gridName)
        {
            return _queries.TryGetValue(pid, out var q)
                   && q.Query == gridName
                   && (DateTime.UtcNow - q.Timestamp).TotalSeconds < 30;
        }

        static void QueryForce(string gridName, ulong pid)
        {
            _queries[pid] = (gridName, DateTime.UtcNow);
        }
    }
}