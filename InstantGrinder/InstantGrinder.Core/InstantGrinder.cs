﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InstantGrinder.Patches;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.Torch;
using VRage.Game;
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

        public bool TryGrindByName(MyPlayer playerOrNull, string gridName, bool force, bool asPlayer, out GrindObjection objection)
        {
            if (!_config.Enabled)
            {
                throw new InvalidOperationException("Plugin not active");
            }

            if (!Utils.TryGetGridGroupByName(gridName, out var gridGroup))
            {
                throw new InvalidOperationException($"Not found: {gridName}");
            }

            return GrindGrids(playerOrNull, gridGroup, force, asPlayer, out objection);
        }

        public bool GrindGridSelected(MyPlayer playerOrNull, bool force, bool asPlayer, out GrindObjection objection)
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
            return GrindGrids(playerOrNull, grids, force, asPlayer, out objection);
        }

        bool GrindGrids(IMyPlayer playerOrNull, IReadOnlyList<MyCubeGrid> gridGroup, bool force, bool asPlayer, out GrindObjection objection)
        {
            // don't let non-owners grind a grid
            var isNormalPlayer = playerOrNull?.IsNormalPlayer() ?? false;
            isNormalPlayer |= asPlayer; // pretend like a normal player as an admin
            if (isNormalPlayer && !playerOrNull.OwnsAll(gridGroup))
            {
                throw new InvalidOperationException("Not yours");
            }

            var tooFar = false;
            foreach (var grid in gridGroup)
            {
                // don't grind inside a safe zone (because it doesn't work)
                foreach (var safeZone in MySessionComponentSafeZones_SafeZones.Value)
                {
                    if (!safeZone.IsOutside(grid.PositionComp.WorldAABB))
                    {
                        throw new InvalidOperationException($"In a safe zone: {grid.DisplayName}");
                    }
                }

                // projector doesn't work either
                if (grid.Physics == null)
                {
                    throw new InvalidOperationException($"Projected grid: {grid.DisplayName}");
                }

                // large grid
                if (grid.GridSizeEnum == MyCubeSize.Large && !force)
                {
                    objection = new GrindObjection("Grinding a large grid can break your character inventory.");
                    return false;
                }

                // distance filter
                if (playerOrNull is MyPlayer p)
                {
                    var gridPosition = Utils.AvgPosition(gridGroup);
                    var playerPosition = p.GetPosition();
                    var distance = Vector3D.Distance(gridPosition, playerPosition);
                    if (distance > _config.MaxDistance)
                    {
                        tooFar = true;

                        if (!force)
                        {
                            objection = new GrindObjection($"Too far: {grid.DisplayName}; you will not receive items.");
                            return false;
                        }
                    }
                }
            }

            // don't grind multiple grids at once, unless specified
            if (gridGroup.Count > 1 && !force)
            {
                var msgBuilder = new StringBuilder();
                msgBuilder.AppendLine("Multiple grids found:");
                foreach (var grid in gridGroup)
                {
                    msgBuilder.AppendLine($" + {grid.DisplayName}");
                }

                objection = new GrindObjection(msgBuilder.ToString());
                return false;
            }

            // don't grind too many items, unless specified
            var itemCount = Utils.GetItemCount(gridGroup);
            if (itemCount > _config.MaxItemCount && !force)
            {
                objection = new GrindObjection($"Too many items: {itemCount}; your character inventory will overflow.");
                return false;
            }

            if (playerOrNull is MyPlayer player && !tooFar)
            {
                GrindGridsIntoPlayerInventory(gridGroup, player);
            }
            else // nobody will receive the items
            {
                var blocks = gridGroup.SelectMany(g => g.CubeBlocks).ToArray();
                foreach (var block in blocks)
                {
                    block.Remove();
                }
            }

            objection = default;
            return true;
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