using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.Torch;
using VRage.Game.ModAPI;
using VRageMath;

namespace InstantGrinder.Core
{
    public sealed class InstantGrinder
    {
        public interface IConfig
        {
            bool Enabled { get; }
            double MaxDistance { get; }
            int MaxItemCount { get; }
        }

        readonly IConfig _config;

        public InstantGrinder(IConfig config)
        {
            _config = config;
        }

        public void TryGrindByName(MyPlayer playerOrNull, string gridName, bool confirmed, ICollection<IGrindObjection> objections)
        {
            if (!_config.Enabled)
            {
                throw new InvalidOperationException("Plugin not active");
            }

            if (!Utils.TryGetGridGroupByName(gridName, out var gridGroup))
            {
                throw new InvalidOperationException($"Not found: {gridName}");
            }

            GrindGrids(playerOrNull, gridGroup, confirmed, objections);
        }

        public void GrindGridSelected(MyPlayer playerOrNull, bool confirmed, ICollection<IGrindObjection> objections)
        {
            if (!_config.Enabled)
            {
                throw new InvalidOperationException("Plugin not active");
            }

            if (playerOrNull == null)
            {
                throw new InvalidOperationException("Character required");
            }

            if (!playerOrNull.TryGetSelectedGrid(out var grid))
            {
                throw new InvalidOperationException("Not found");
            }

            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(grid);
            var grids = gridGroup.Nodes.Select(n => n.NodeData).ToArray();
            GrindGrids(playerOrNull, grids, confirmed, objections);
        }

        void GrindGrids(IMyPlayer playerOrNull, IReadOnlyList<MyCubeGrid> gridGroup, bool confirmed, ICollection<IGrindObjection> objections)
        {
            // don't let non-owners grind a grid
            var isNormalPlayer = playerOrNull?.IsNormalPlayer() ?? false;
            if (isNormalPlayer && !playerOrNull.OwnsAll(gridGroup))
            {
                throw new InvalidOperationException("Not yours");
            }

            var farGrids = new List<MyCubeGrid>();
            foreach (var grid in gridGroup)
            {
                // projector doesn't work
                if (grid.Physics == null)
                {
                    throw new InvalidOperationException($"Projection: {grid.DisplayName}");
                }

                // distance filter
                if (playerOrNull is MyPlayer p)
                {
                    var gridPosition = Utils.AvgPosition(gridGroup);
                    var playerPosition = p.GetPosition();
                    var distance = Vector3D.Distance(gridPosition, playerPosition);
                    if (distance > _config.MaxDistance)
                    {
                        farGrids.Add(grid);
                    }
                }
            }

            var gridNames = gridGroup.Select(g => g.DisplayName).ToArray();

            // don't grind multiple grids at once, unless specified
            if (gridGroup.Count > 1)
            {
                objections.Add(new GrindObjectionMultipleGrinds(gridNames));
            }

            // distance filter
            if (farGrids.Count > 0)
            {
                objections.Add(new GrindObjectionUnrecoverable(gridNames));
            }

            // don't grind too many items, unless specified
            var itemCount = gridGroup.Sum(g => Utils.GetItemCount(g));
            if (itemCount > _config.MaxItemCount)
            {
                objections.Add(new GrindObjectionOverflowItems(itemCount));
            }

            if (!confirmed) return;

            if (playerOrNull is MyPlayer player && farGrids.Count == 0)
            {
                GrindGridsIntoPlayerInventory(gridGroup, player);
            }
            else // nobody will receive the items
            {
                foreach (var grid in gridGroup)
                {
                    grid.Close();
                }
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
                Utils.DeconstructStockpile(block, playerInventory);
            }
            
            foreach (var grid in gridGroup)
            {
                grid.Close();
            }

            playerInventory.Refresh();
        }
    }
}