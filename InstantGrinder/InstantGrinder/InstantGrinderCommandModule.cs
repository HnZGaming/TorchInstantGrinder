using System;
using System.Text;
using NLog;
using Sandbox.Game;
using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using TorchUtils;
using VRage.Game.ModAPI;
using VRageMath;

namespace InstantGrinder
{
    [Category(Cmd_Category)]
    public sealed class InstantGrinderCommandModule : CommandModule
    {
        const string Cmd_Category = "grind";
        const string Cmd_GrindByName = "name";
        const string Cmd_GrindByNameAdmin = "name_admin";
        const string Cmd_Help = "help";
        static readonly string HelpSentence = $"See !{Cmd_Category} {Cmd_Help}.";

        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        InstantGrinderPlugin Plugin => (InstantGrinderPlugin) Context.Plugin;

        [Command(Cmd_GrindByName, "Grind a grid and transfer components to player's character inventory.")]
        [Permission(MyPromoteLevel.None)]
        public void GrindByName(string gridName) => this.CatchAndReport(() =>
        {
            var option = new GrindByNameCommandOption(Context.Args);

            var player = Context.Player;
            if (player == null)
            {
                throw new Exception("Can only be called by a player");
            }

            if (!Plugin.TryGetGridGroupByName(gridName, out var gridGroup))
            {
                if (Plugin.TryGetPlayerByName(gridName, out var foundPlayer))
                {
                    var myPlayerName = Context.Player.DisplayName;
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

            if (!Plugin.CanGrind(player.IdentityId, gridGroup))
            {
                Context.Respond($"Grid found, but not yours: \"{gridName}\". You need to be a \"big owner\". {HelpSentence}", Color.Yellow);
                return;
            }

            if (gridGroup.Length > 1 && !option.Force)
            {
                var msgBuilder = new StringBuilder();
                msgBuilder.AppendLine("Multiple grids found:");
                foreach (var grid in gridGroup)
                {
                    msgBuilder.AppendLine($" + {grid.DisplayName}");
                }

                msgBuilder.AppendLine();
                msgBuilder.AppendLine($"To proceed, type !{Cmd_Category} {Cmd_GrindByName} \"{gridName}\" {GrindByNameCommandOption.Prefix}{GrindByNameCommandOption.Key_Force}");
                Context.Respond(msgBuilder.ToString(), Color.Yellow);
                return;
            }

            Plugin.GridGridGroup((MyPlayer) player, gridGroup);

            Context.Respond($"Finished grinding: \"{gridName}\"", Color.White);

            var playerInventory = (MyInventory) player.Character.GetInventory();
            if (playerInventory.CurrentMass > playerInventory.MaxMass ||
                playerInventory.CurrentVolume > playerInventory.MaxVolume)
            {
                Context.Respond("Your character inventory is more than full. Store your items as soon as possible. Your items may be deleted anytime.", Color.Yellow);
            }
        });

        [Command(Cmd_GrindByNameAdmin, "Grind a grid and transfer components to player's character inventory.")]
        [Permission(MyPromoteLevel.Admin)]
        public void GrindByNameAdmin(string playerName, string gridName) => this.CatchAndReport(() =>
        {
            if (!Plugin.TryGetGridGroupByName(gridName, out var gridGroup))
            {
                Context.Respond($"Grid not found by name: \"{gridName}\". {HelpSentence}", Color.Yellow);
                return;
            }

            var player = MySession.Static.Players.GetPlayerByName(playerName);
            if (player == null)
            {
                Context.Respond($"Player not found by name: \"{playerName}\". {HelpSentence}.", Color.Yellow);
                return;
            }

            Plugin.GridGridGroup(player, gridGroup);
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