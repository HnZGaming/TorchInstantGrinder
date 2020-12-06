using System;
using System.Text;
using InstantGrinder.Reflections;
using NLog;
using Sandbox.Game;
using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.Torch;
using VRage.Game.ModAPI;
using VRageMath;

namespace InstantGrinder
{
    [Category(Cmd_Category)]
    public sealed class InstantGrinderCommandModule : CommandModule
    {
        const string Cmd_Category = "grind";
        const string Cmd_GrindByName = "name";
        const string Cmd_Enable = "enable";
        const string Cmd_Disable = "disable";
        const string Cmd_Help = "help";
        static readonly string HelpSentence = $"See !{Cmd_Category} {Cmd_Help}.";

        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        InstantGrinderPlugin Plugin => (InstantGrinderPlugin) Context.Plugin;

        [Command(Cmd_Enable, "Enable plugin.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Enable() => this.CatchAndReport(() =>
        {
            Plugin.IsEnabled = true;
            Context.Respond("Enabled Instant Grinder plugin.");
        });

        [Command(Cmd_Disable, "Disable plugin.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Disable() => this.CatchAndReport(() =>
        {
            Plugin.IsEnabled = false;
            Context.Respond("Disabled Instant Grinder plugin.");
        });

        [Command(Cmd_GrindByName, "Grind a grid and transfer components to player's character inventory.")]
        [Permission(MyPromoteLevel.None)]
        public void GrindByName(string gridName) => this.CatchAndReport(() =>
        {
            var option = new GrindByNameCommandOption(Context.Args);
            Log.Info($"force: {option.Force}, as_player: {option.AsPlayer}");

            if (!Plugin.IsEnabled)
            {
                Context.Respond("Plugin is disabled.", Color.Red);
                return;
            }

            var player = Context.Player;

            // "Grind player"
            if (!Plugin.TryGetGridGroupByName(gridName, out var gridGroup))
            {
                if (Plugin.TryGetPlayerByName(gridName, out var foundPlayer))
                {
                    var myPlayerName = player?.DisplayName ?? "<Server>";
                    var msgBuilder = new StringBuilder();
                    msgBuilder.AppendLine("WARNING!!");
                    msgBuilder.AppendLine($"{myPlayerName} tried to grind you! xD");
                    msgBuilder.AppendLine("Grind them back with command:");
                    msgBuilder.AppendLine($">> !grind name \"{myPlayerName}\"");
                    SendMessageToPlayer(foundPlayer, Color.Red, msgBuilder.ToString());

                    Context.Respond($"You've sent a death threat to {gridName}.");
                    return;
                }

                Context.Respond($"Grid not found by name: \"{gridName}\". Try double quotes (\"foo bar\"). {HelpSentence}", Color.Yellow);
                return;
            }

            // Check ownership
            if (option.AsPlayer || player?.PromoteLevel == MyPromoteLevel.None)
            {
                if (!player.OwnsAll(gridGroup))
                {
                    Context.Respond($"Grid found, but not yours: \"{gridName}\". You need to be a \"big owner\". {HelpSentence}", Color.Yellow);
                    return;
                }
            }

            // Check safe zones
            var safeZones = MySessionComponentSafeZones_SafeZones.Value;
            foreach (var safeZone in safeZones)
            foreach (var grid in gridGroup)
            {
                var isOutside = safeZone.IsOutside(grid);
                if (!isOutside) // Colliding with a safe zone
                {
                    Context.Respond($"Grid found, but in a safe zone: \"{gridName}\". You need to exit the safe zone. {HelpSentence}", Color.Yellow);
                    return;
                }
            }

            // Fool-proof for when multiple grids would be ground down
            if (!option.Force && gridGroup.Length > 1)
            {
                var msgBuilder = new StringBuilder();
                msgBuilder.AppendLine("Multiple grids found:");
                foreach (var grid in gridGroup)
                {
                    msgBuilder.AppendLine($" + {grid.DisplayName}");
                }

                msgBuilder.AppendLine();
                msgBuilder.AppendLine($"To proceed, run the command with {GrindByNameCommandOption.ForceOption}");
                Context.Respond(msgBuilder.ToString(), Color.Yellow);
                return;
            }

            if (!option.Force && !Plugin.ValidateInventoryItemCount(gridGroup, out var itemCount))
            {
                var msgBuilder = new StringBuilder();
                msgBuilder.AppendLine($"Too many items found in this grid's inventories: {itemCount}.");
                msgBuilder.AppendLine("Some of your items can get lost if proceeded to grind this grid.");
                msgBuilder.AppendLine($"To proceed, run the command with {GrindByNameCommandOption.ForceOption}");
                Context.Respond(msgBuilder.ToString(), Color.Yellow);
                return;
            }

            if (player == null)
            {
                Plugin.GrindGridGroup(gridGroup);
            }
            else
            {
                Plugin.GrindGridGroup((MyPlayer) player, gridGroup);
            }

            Context.Respond($"Finished grinding: \"{gridName}\"", Color.White);

            // Warn inventory maxed out
            if (player != null)
            {
                var playerInventory = (MyInventory) player.Character.GetInventory();
                if (playerInventory.CurrentMass > playerInventory.MaxMass ||
                    playerInventory.CurrentVolume > playerInventory.MaxVolume)
                {
                    Context.Respond("Your character inventory is more than full. Store your items as soon as possible. Your items may be deleted anytime.", Color.Yellow);
                }
            }
        });

        void SendMessageToPlayer(MyPlayer player, Color color, string message)
        {
            var chat = Plugin.Torch.Managers.GetManager<IChatManagerServer>();
            chat.SendMessageAsOther(null, message, color, player.SteamId());
        }

        [Command(Cmd_Help, "Show help.")]
        [Permission(MyPromoteLevel.None)]
        public void Help()
        {
            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine("Command list:");

            var commands = Context.Torch.GetPluginCommands(Cmd_Category, Context.Player?.PromoteLevel);
            foreach (var command in commands)
            {
                msgBuilder.AppendLine();
                msgBuilder.AppendLine($"{command.SyntaxHelp}");
                msgBuilder.AppendLine($" -- {command.HelpText}");
            }

            Context.Respond(msgBuilder.ToString());
        }
    }
}