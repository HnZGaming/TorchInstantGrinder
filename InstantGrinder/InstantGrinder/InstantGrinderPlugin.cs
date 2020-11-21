using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using InstantGrinder.Reflections;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Torch;
using InfluxDb;
using Torch.API;
using Torch.API.Plugins;
using Torch.Utils;
using TorchUtils;
using VRage.Game.Entity;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using System.Collections;
using VRage.Replication;
using VRage.Collections;

namespace InstantGrinder
{
    public sealed class InstantGrinderPlugin : TorchPluginBase, IWpfPlugin
    {
        private const string CLIENT_DATA_TYPE_NAME = "VRage.Network.MyClient, VRage";

        [ReflectedGetter(Name = "m_clientStates")]
        private static Func<MyReplicationServer, IDictionary> _clientStates;

        [ReflectedGetter(TypeName = CLIENT_DATA_TYPE_NAME, Name = "Replicables")]
        private static Func<object, MyConcurrentDictionary<IMyReplicable, MyReplicableClientData>> _replicables;

        [ReflectedMethod(Name = "RemoveForClient", OverrideTypeNames = new[] { null, CLIENT_DATA_TYPE_NAME, null })]
        private static Action<MyReplicationServer, IMyReplicable, object, bool> _removeForClient;

        [ReflectedMethod(Name = "ForceReplicable")]
        private static Action<MyReplicationServer, IMyReplicable, Endpoint> _forceReplicable;

        public static Func<MyReplicationServer, IDictionary> ClientStates { get => _clientStates; set => _clientStates = value; }
        public static Func<object, MyConcurrentDictionary<IMyReplicable, MyReplicableClientData>> Replicables { get => _replicables; set => _replicables = value; }
        public static Action<MyReplicationServer, IMyReplicable, object, bool> RemoveForClient { get => _removeForClient; set => _removeForClient = value; }
        public static Action<MyReplicationServer, IMyReplicable, Endpoint> ForceReplicable { get => _forceReplicable; set => _forceReplicable = value; }

        Persistent<InstantGrinderConfig> _config;
        UserControl _userControl;
        public bool GridInventoryIsEmpty = false;
        public bool GridWasGrinded = false;

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

        private void PlayerRefresh(MyPlayer player)
        {
            var playerEndpoint = new Endpoint(player.Id.SteamId, 0);
            var replicationServer = (MyReplicationServer)MyMultiplayer.ReplicationLayer;
            object clientData;

            try
            {
                clientData = ClientStates.Invoke(replicationServer)[playerEndpoint];
            }
            catch
            {
                return;
            }

            List<IMyReplicable> lists = new List<IMyReplicable>(Replicables.Invoke(clientData).Count);
            foreach (var pair in Replicables.Invoke(clientData))
            {
                lists.Add(pair.Key);
            }

            foreach (var replicable in lists)
            {
                RemoveForClient.Invoke(replicationServer, replicable, clientData, true);
                ForceReplicable.Invoke(replicationServer, replicable, playerEndpoint);
            }
            player.Character.GetInventory().Refresh();
        }

        public void GridGridGroup(MyPlayer player, IEnumerable<MyCubeGrid> gridGroup)
        {
            var playerInventory = player.Character.GetInventory();
            var blocks = gridGroup.SelectMany(g => g.CubeBlocks).ToArray();
            GridWasGrinded = false;
            var InventoryMaxvolume = 630000000;
            bool ItemsMoved = false;

            // turn off all blocks on grid.
            foreach (var grid in gridGroup.OfType<MyCubeGrid>().Where(x => x.Projector == null))
            {
                foreach (var block in grid.GetFatBlocks().OfType<MyFunctionalBlock>())
                {
                    if (block != null && block.Enabled == true)
                    {
                        block.Enabled = false;
                    }
                }
            }

            // save inventory items first
            foreach (var block in blocks)
            {
                if (block.FatBlock == null) continue;

                if (playerInventory.GetItemsCount() >= 65 ||
                    playerInventory.CurrentMass >= playerInventory.MaxMass - 50000 ||
                    playerInventory.CurrentVolume >= InventoryMaxvolume)
                {
                    continue;
                }
                DeleteItems(block.FatBlock);
                CopyItems(block.FatBlock, playerInventory);
                ItemsMoved = true;
            }
            playerInventory.Refresh();

            foreach (var block in blocks)
            {
                if (block.FatBlock == null) continue;
                GridInventoryRefresh(block.FatBlock);
            }

            if (GridInventoryIsEmpty && playerInventory.GetItemsCount() <= 20)
            {
                // then grind them down
                GridWasGrinded = true;
                foreach (var block in blocks)
                {
                    GrindBlock(block, playerInventory);
                    ItemsMoved = true;
                }
                playerInventory.Refresh();
            }

            // report
            InfluxDbPointFactory
                .Measurement("instant_grinder")
                .Tag("player_name", player.DisplayName)
                .Tag("steam_id", $"{player.SteamId()}")
                .Field("exec_count", 1)
                .Field("grid_count", gridGroup.Count())
                .Field("block_count", blocks.Length)
                .Write();

            //MyHud.ChangedInventoryItems.Update();

            //           if (ItemsMoved)
            //               PlayerRefresh(player);
        }

        private void DeleteItems(MyEntity src)
        {
            if (!src.HasInventory) return;

            string typeToDelete1 = "MyObjectBuilder_PhysicalGunObject";
            string typeToDelete2 = "MyObjectBuilder_OxygenContainerObject";
            string typeToDelete3 = "MyObjectBuilder_GasContainerObject";

            for (var index = 0; index < src.InventoryCount; index++)
            {
                var inventory = src.GetInventory(index);
                if (inventory.Empty())
                    continue;

                // clean tools from source grid.
                try
                {
                    var guns = src.GetInventory().ItemCount;
                    for (var y = 0; y < guns; y++)
                    {
                        var ii = inventory.GetItemAt(y);
                        if (ii.HasValue && ii.Value.Type.TypeId == typeToDelete1)
                        {
                            inventory.RemoveItemsAt(y, null);
                            y--;
                            guns--;
                        }
                    }

                    var Oxygen = src.GetInventory().ItemCount;
                    for (var y = 0; y < Oxygen; y++)
                    {
                        var ii = inventory.GetItemAt(y);
                        if (ii.HasValue && ii.Value.Type.TypeId == typeToDelete2)
                        {
                            inventory.RemoveItemsAt(y, null);
                            y--;
                            Oxygen--;
                        }
                    }

                    var Hydrogen = src.GetInventory().ItemCount;
                    for (var y = 0; y < Hydrogen; y++)
                    {
                        var ii = inventory.GetItemAt(y);
                        if (ii.HasValue && ii.Value.Type.TypeId == typeToDelete3)
                        {
                            inventory.RemoveItemsAt(y, null);
                            y--;
                            Hydrogen--;
                        }
                    }
                }
                catch
                {
                    return;
                }
            }
        }

        private void CopyItems(MyEntity src, MyInventory dst)
        {
            if (!src.HasInventory) return;
 
            for (var index = 0; index < src.InventoryCount; index++)
            {
				var inventory = src.GetInventory(index);
                if (inventory.Empty())
                {
                    GridInventoryIsEmpty = true;
                    continue;
                }

                GridInventoryIsEmpty = false;

                foreach (var item in inventory.GetItems())
                {
                    if (item.Amount <= 0)
                        continue;

                    dst.AddItemsInternal(item.Content, item.Amount);
                }
                inventory.ClearItems();
            }
        }

        private void GridInventoryRefresh(MyEntity src)
        {
            if (!src.HasInventory) return;

            for (var index = 0; index < src.InventoryCount; index++)
            {
                var inventory = src.GetInventory(index);
                inventory.Refresh();
            }
        }

        private void GrindBlock(MySlimBlock src, MyInventory dst)
        {
            src.DeconstructStockpile(float.MaxValue, dst);

            if (src.Value() == null) return;

            foreach (var item in src.Value().GetItems())
            {
                dst.AddItemsInternal(item.Content, item.Amount);
            }
            src.CubeGrid.RazeBlock(src.Min);
        }
    }
}