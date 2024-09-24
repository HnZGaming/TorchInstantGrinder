using System;
using System.Collections.Generic;
using System.Text;
using InstantGrinder.Core;
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

            var objections = new List<GrindObjection>();
            if (!Grinder.TryGrindByName(player, gridName, force, asPlayer, objections))
            {
                RespondWithObjections(gridName, pid, objections);
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

            var objections = new List<GrindObjection>();
            if (!Grinder.GrindGridSelected(Context.Player as MyPlayer, force, asPlayer, objections))
            {
                RespondWithObjections("this", pid, objections);
                return;
            }

            Context.Respond("Finished grinding grid");
        });

        void RespondWithObjections(string command, ulong steamId, IReadOnlyList<GrindObjection> objections)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Failed grinding:");
            for (var i = 0; i < objections.Count; i++)
            {
                var objection = objections[i];
                sb.AppendLine($"[{i + 1}] {objection.Message}");
            }

            sb.AppendLine("To proceed anyway, type the same command again.");

            Context.Respond(sb.ToString(), Color.Yellow);
            QueryForce(command, steamId);
        }

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