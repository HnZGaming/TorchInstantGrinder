using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InstantGrinder.Core;
using NLog;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;
using VRageMath;

namespace InstantGrinder
{
    [Category("grind")]
    public sealed class InstantGrinderCommandModule : CommandModule
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly ConfirmationCollection ConfirmationCollection = new();

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
            var confirmed = ConfirmationCollection.IsConfirmation(pid, gridName);
            var objections = new List<IGrindObjection>();
            Grinder.TryGrindByName(player, gridName, confirmed, asPlayer, objections);
            Respond(gridName, pid, confirmed, objections);
        });

        [Command("this", "Grind a grid that the player is looking at or seated on.")]
        [Permission(MyPromoteLevel.None)]
        public void GrindThis(bool asPlayer = false) => this.CatchAndReport(() =>
        {
            var player = Context.Player as MyPlayer;
            var pid = player?.SteamId() ?? 0L;
            var confirmed = ConfirmationCollection.IsConfirmation(pid, "this");
            var objections = new List<IGrindObjection>();
            Grinder.GrindGridSelected(Context.Player as MyPlayer, confirmed, asPlayer, objections);
            Respond("this", pid, confirmed, objections);
        });

        void Respond(string command, ulong steamId, bool confirmed, List<IGrindObjection> objections)
        {
            var sb = new StringBuilder();

            if (objections.Count > 0)
            {
                FormatObjections(sb, objections);
            }

            if (confirmed)
            {
                Context.Respond("Finished grinding grid");

                ConfirmationCollection.Clear(steamId);
            }
            else
            {
                sb.AppendLine("Type the same command again to proceed.");
                Context.Respond(sb.ToString(), Color.Yellow);

                ConfirmationCollection.PendConfirmation(command, steamId);
            }
        }

        static void FormatObjections(StringBuilder sb, IReadOnlyList<IGrindObjection> objections)
        {
            Log.Info(objections.Select(o => o.GetType()).ToStringSeq());

            sb.AppendLine($"Failed grinding ({objections.Count}); reasons:");

            for (var i = 0; i < objections.Count; i++)
            {
                switch (objections[i])
                {
                    case GrindObjectionMultipleGrinds objection:
                    {
                        sb.AppendLine($"[{i + 1}] Grinding multiple grids ({objection.gridNames.Length}):");
                        for (var j = 0; j < objection.gridNames.Length; j++)
                        {
                            sb.AppendLine($"   <{j + 1}> {objection.gridNames[j]}");
                        }

                        break;
                    }
                    case GrindObjectionUnrecoverable objection:
                    {
                        sb.AppendLine($"[{i + 1}] Too far to recover components ({objection.gridNames.Length}):");
                        for (var j = 0; j < objection.gridNames.Length; j++)
                        {
                            sb.AppendLine($"   <{j + 1}> {objection.gridNames[j]}");
                        }

                        break;
                    }
                    case GrindObjectionOverflowItems objection:
                    {
                        sb.AppendLine($"[{i + 1}] Overflowing character inventory: {objection.itemCount} items");

                        break;
                    }
                }
            }
        }

        [Command("clear", "Clear confirmation collections")]
        [Permission(MyPromoteLevel.None)]
        public void ClearConfirmationCollection() => this.CatchAndReport(() =>
        {
            var player = Context.Player as MyPlayer;
            var pid = player?.SteamId() ?? 0L;
            if (pid != 0)
            {
                ConfirmationCollection.Clear(pid);
            }
            else
            {
                ConfirmationCollection.ClearAll();
            }
        });
    }
}