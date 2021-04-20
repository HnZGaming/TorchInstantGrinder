using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using InstantGrinder.Patches;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Utils.Torch;
using VRage.Game.Entity;

namespace InstantGrinder
{
    public sealed class InstantGrinderPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        Persistent<InstantGrinderConfig> _config;
        UserControl _userControl;

        public bool IsEnabled
        {
            get => _config.Data.Enabled;
            set => _config.Data.Enabled = value;
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var configFilePath = this.MakeConfigFilePath();
            _config = Persistent<InstantGrinderConfig>.Load(configFilePath);
        }

        public UserControl GetControl() => _config.GetOrCreateUserControl(ref _userControl);

        public bool TryGetGridGroupByName(string gridName, out MyCubeGrid[] foundGridGroup)
        {
            foreach (var group in MyCubeGridGroups.Static.Logical.Groups)
            foreach (var node in group.Nodes)
            {
                var grid = node.NodeData;
                if (grid.DisplayName == gridName)
                {
                    foundGridGroup = group.Nodes.Select(n => n.NodeData).ToArray();
                    return true;
                }
            }

            foundGridGroup = null;
            return false;
        }

        public bool TryGetPlayerByName(string playerName, out MyPlayer player)
        {
            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers().ToArray();
            foreach (var onlinePlayer in onlinePlayers)
            {
                if (onlinePlayer.DisplayName == playerName)
                {
                    player = onlinePlayer;
                    return true;
                }
            }

            player = default;
            return false;
        }

        public void GrindGridGroup(IEnumerable<MyCubeGrid> gridGroup)
        {
            var blocks = gridGroup.SelectMany(g => g.CubeBlocks).ToArray();
            foreach (var block in blocks)
            {
                GrindBlock(block);
            }
        }

        public bool ValidateInventoryItemCount(IEnumerable<MyCubeGrid> gridGroup, out int itemCount)
        {
            var itemIds = new HashSet<uint>();
            var blocks = gridGroup.SelectMany(g => g.CubeBlocks).ToArray();
            foreach (var block in blocks)
            {
                if (!(block.FatBlock is MyCubeBlock fatBlock)) continue;

                for (var i = 0; i < fatBlock.InventoryCount; i++)
                {
                    var inventory = fatBlock.GetInventory(i);
                    foreach (var item in inventory.GetItems())
                    {
                        itemIds.Add(item.ItemId);
                    }
                }
            }

            itemCount = itemIds.Count;
            return itemCount < 50;
        }

        public void GrindGridGroup(MyPlayer player, IEnumerable<MyCubeGrid> gridGroup)
        {
            var playerInventory = player.Character.GetInventory();
            var blocks = gridGroup.SelectMany(g => g.CubeBlocks).ToArray();

            // save inventory items first
            foreach (var block in blocks)
            {
                if (block.FatBlock == null) continue;
                CopyItems(block.FatBlock, playerInventory);
            }

            playerInventory.Refresh();

            // then grind them down
            foreach (var block in blocks)
            {
                GrindBlock(block, playerInventory);
            }

            playerInventory.Refresh();
        }

        void CopyItems(MyEntity src, MyInventory dst)
        {
            if (!src.HasInventory) return;

            for (var index = 0; index < src.InventoryCount; index++)
            {
                var inventory = src.GetInventory(index);
                if (inventory.Empty()) continue;

                foreach (var item in inventory.GetItems())
                {
                    dst.AddItemsInternal(item.Content, item.Amount);
                }

                inventory.ClearItems();
            }
        }

        void GrindBlock(MySlimBlock src, MyInventory dst)
        {
            src.DeconstructStockpile(float.MaxValue, dst);

            var stockpile = src.Value();
            if (stockpile == null) return;

            foreach (var item in stockpile.GetItems())
            {
                dst.AddItemsInternal(item.Content, item.Amount);
            }

            src.CubeGrid.RazeBlock(src.Min);
        }

        void GrindBlock(MySlimBlock block)
        {
            block.CubeGrid.RazeBlock(block.Min);
        }
    }
}