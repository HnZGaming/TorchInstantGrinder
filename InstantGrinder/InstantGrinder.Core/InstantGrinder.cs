using System.Collections.Generic;
using System.Linq;
using System.Text;
using InstantGrinder.Patches;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;
using VRageMath;

namespace InstantGrinder.Core
{
    public sealed class InstantGrinder
    {
        public interface IConfig
        {
            double MaxDistance { get; }
        }

        readonly IConfig _config;

        public InstantGrinder(IConfig config)
        {
            _config = config;
        }

        public void GrindGridByName(MyPlayer playerOrNull, string gridName, bool force, bool asPlayer)
        {
            if (!Utils.TryGetGridGroupByName(gridName, out var gridGroup))
            {
                throw new UserFacingException($"Not found: {gridName}");
            }

            // don't let non-owners grind a grid
            var isNormalPlayer = playerOrNull?.IsNormalPlayer() ?? false;
            isNormalPlayer |= asPlayer; // pretend like a normal player as an admin
            if (isNormalPlayer && !playerOrNull.OwnsAll(gridGroup))
            {
                throw new UserFacingException($"Not yours: {gridName}");
            }

            foreach (var safeZone in MySessionComponentSafeZones_SafeZones.Value)
            foreach (var grid in gridGroup)
            {
                // don't grind inside a safe zone (because it doesn't work)
                if (!safeZone.IsOutside(grid))
                {
                    throw new UserFacingException($"In a safe zone: {gridName}");
                }

                // projector doesn't work either
                if (grid.Physics == null)
                {
                    throw new UserFacingException($"Projected grid: {gridName}");
                }

                // distance filter
                if (playerOrNull is MyPlayer p)
                {
                    var gridPosition = Utils.AvgPosition(gridGroup);
                    var playerPosition = p.GetPosition();
                    var distance = Vector3D.Distance(gridPosition, playerPosition);
                    if (distance > _config.MaxDistance)
                    {
                        throw new UserFacingException($"Too far: {gridName}");
                    }
                }
            }

            // don't grind multiple grids at once, unless specified
            if (gridGroup.Length > 1 && !force)
            {
                var msgBuilder = new StringBuilder();
                msgBuilder.AppendLine("Multiple grids found:");
                foreach (var grid in gridGroup)
                {
                    msgBuilder.AppendLine($" + {grid.DisplayName}");
                }

                throw new UserFacingException(msgBuilder.ToString());
            }

            // don't grind too many items, unless specified
            var itemCount = Utils.GetInventoryItemCount(gridGroup);
            if (itemCount > 50 && !force)
            {
                var msgBuilder = new StringBuilder();
                msgBuilder.AppendLine($"Too many items in inventories: {itemCount}");
                throw new UserFacingException(msgBuilder.ToString());
            }

            if (playerOrNull is MyPlayer player)
            {
                GrindGridsIntoPlayerInventory(gridGroup, player);
            }
            else // nobody will receive the items
            {
                GrindGrids(gridGroup);
            }
        }

        void GrindGrids(IEnumerable<MyCubeGrid> gridGroup)
        {
            foreach (var block in gridGroup.SelectMany(g => g.CubeBlocks))
            {
                Utils.GrindBlock(block);
            }
        }

        void GrindGridsIntoPlayerInventory(IEnumerable<MyCubeGrid> gridGroup, MyPlayer player)
        {
            var playerInventory = player.Character.GetInventory();
            var blocks = gridGroup.SelectMany(g => g.CubeBlocks).ToArray();

            // save inventory items first
            foreach (var block in blocks)
            {
                if (block.FatBlock == null) continue;
                Utils.CopyItemsIntoInventory(block.FatBlock, playerInventory);
            }

            playerInventory.Refresh();

            // then grind them down
            foreach (var block in blocks)
            {
                Utils.GrindBlockIntoInventory(block, playerInventory);
            }

            playerInventory.Refresh();
        }
    }
}