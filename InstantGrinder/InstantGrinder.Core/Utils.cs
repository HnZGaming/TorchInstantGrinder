using System.Collections.Generic;
using System.Linq;
using InstantGrinder.Patches;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace InstantGrinder.Core
{
    public static class Utils
    {
        public static bool TryGetGridGroupByName(string gridName, out MyCubeGrid[] foundGridGroup)
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

        public static int GetItemCount(IEnumerable<MyCubeGrid> gridGroup)
        {
            var itemTypeIds = new HashSet<MyDefinitionId>();
            var blocks = gridGroup.SelectMany(g => g.CubeBlocks);
            foreach (var block in blocks)
            {
                foreach (var compDef in block.BlockDefinition.Components)
                {
                    itemTypeIds.Add(compDef.Definition.Id);
                }

                if (block.FatBlock is { } fatBlock)
                {
                    for (var i = 0; i < fatBlock.InventoryCount; i++)
                    {
                        var inventory = fatBlock.GetInventory(i);
                        foreach (var item in inventory.GetItems())
                        {
                            itemTypeIds.Add(item.Content.GetObjectId());
                        }
                    }
                }
            }

            return itemTypeIds.Count;
        }

        public static void CopyItemsIntoInventory(MyEntity src, MyInventory dst)
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

        public static void GrindBlockIntoInventory(MySlimBlock src, MyInventory dst)
        {
            src.DeconstructStockpile(float.MaxValue, dst);

            var stockpile = src.Value();
            if (stockpile == null) return;

            foreach (var item in stockpile.GetItems())
            {
                dst.AddItemsInternal(item.Content, item.Amount);
            }

            GrindBlock(src);
        }

        public static void GrindBlock(MySlimBlock block)
        {
            block.CubeGrid.RazeBlock(block.Min);
        }

        public static Vector3D AvgPosition(IReadOnlyList<IMyEntity> entities)
        {
            var sum = entities.Aggregate(Vector3D.Zero, (s, n) => s + n.GetPosition());
            return sum / entities.Count;
        }
    }
}