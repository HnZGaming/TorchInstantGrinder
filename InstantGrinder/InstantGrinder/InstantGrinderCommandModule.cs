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
        static readonly ConfirmQuery ConfirmQuery = new();

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
            var force = ConfirmQuery.IsConfirming(pid, gridName);

            var objections = new List<IGrindObjection>();
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
            var force = ConfirmQuery.IsConfirming(pid, "this");

            var objections = new List<IGrindObjection>();
            if (!Grinder.GrindGridSelected(Context.Player as MyPlayer, force, asPlayer, objections))
            {
                RespondWithObjections("this", pid, objections);
                return;
            }

            Context.Respond("Finished grinding grid");
        });

        void RespondWithObjections(string command, ulong steamId, IReadOnlyList<IGrindObjection> objections)
        {
            Log.Info(objections.Select(o=>o.GetType()).ToStringSeq());
            
            var sb = new StringBuilder();
            sb.AppendLine($"Failed grinding ({objections.Count} reasons):");

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

            sb.AppendLine("To proceed anyway, type the same command again.");

            Context.Respond(sb.ToString(), Color.Yellow);
            ConfirmQuery.QueryConfirm(command, steamId);
        }

        [Command("clear", "Clear confirmation queue")]
        [Permission(MyPromoteLevel.None)]
        public void ClearConfirmQueue() => this.CatchAndReport(() =>
        {
            var player = Context.Player as MyPlayer;
            var pid = player?.SteamId() ?? 0L;
            if (pid != 0)
            {
                ConfirmQuery.Clear(pid);
            }
            else
            {
                ConfirmQuery.ClearAll();
            }
        });
    }
}