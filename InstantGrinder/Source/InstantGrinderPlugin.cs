using System.Collections.Generic;
using System.Linq;
using InstantGrinder.Reflections;
using InstantGrinder.Utils;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using VRage.Game.Entity;
using Torch.API.Managers;
using Torch.Server.InfluxDb;

namespace InstantGrinder
{
    public sealed class InstantGrinderPlugin : TorchPluginBaseEx
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        InfluxDbClient _dbClient;

        protected override void OnGameLoaded()
        {
            var dbManager = Torch.CurrentSession.Managers.GetManager<InfluxDbManager>();
            _dbClient = dbManager.Client;
        }

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

        public bool CanGrind(long playerId, IEnumerable<MyCubeGrid> gridGroup)
        {
            foreach (var grid in gridGroup)
            {
                if (!grid.BigOwners.Any()) continue;
                if (!grid.BigOwners.Contains(playerId)) return false;
            }

            return true;
        }

        public void GridGridGroup(MyPlayer player, IEnumerable<MyCubeGrid> gridGroup)
        {
            var playerInventory = player.Character.GetInventory();
            var blocks = gridGroup.SelectMany(g => g.CubeBlocks).ToArray();

            // save inventory items first
            foreach (var block in blocks)
            {
                if (block.FatBlock == null) continue;
                CopyItems(block.FatBlock, playerInventory);
            }

            // then grind them down
            foreach (var block in blocks)
            {
                GrindBlock(block, playerInventory);
            }

            // report
            var point = _dbClient
                .MakePointIn("instant_grinder")
                .Tag("player_name", player.DisplayName)
                .Tag("steam_id", $"{player.SteamId()}")
                .Field("exec_count", 1)
                .Field("grid_count", gridGroup.Count())
                .Field("block_count", blocks.Length);

            _dbClient.WritePoints(point);
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

            var stockpile = src.GetStockpile();
            if (stockpile == null) return;

            foreach (var item in stockpile.GetItems())
            {
                dst.AddItemsInternal(item.Content, item.Amount);
            }

            src.CubeGrid.RazeBlock(src.Min);
        }
    }
}