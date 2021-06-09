using System;
using System.Reflection;
using Sandbox.Game;
using VRage;

namespace InstantGrinder.Patches
{
    public static class MyInventory_TransferItemInternal
    {
        const string MethodName = "TransferItemsInternal";
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;

        static readonly Type[] ParameterTypes =
        {
            typeof(MyInventory),
            typeof(MyInventory),
            typeof(uint),
            typeof(bool),
            typeof(int),
            typeof(MyFixedPoint),
        };

        static readonly MethodInfo Method = typeof(MyInventory).GetMethod(MethodName, Flags, null, ParameterTypes, null);
        static readonly object[] Args = new object[ParameterTypes.Length];

        public static void TransferItemInternal(
            this MyInventory src,
            MyInventory dst,
            uint srcItemId,
            bool spawn = false,
            int destItemIndex = -1,
            MyFixedPoint? amount = null)
        {
            lock (Args)
            {
                Args[0] = src;
                Args[1] = dst;
                Args[2] = srcItemId;
                Args[3] = spawn;
                Args[4] = destItemIndex;
                Args[5] = amount ?? src.GetItemByID(srcItemId)?.Amount ?? 0;
                Method.Invoke(null, Flags, null, Args, null);
                Array.Clear(Args, 0, Args.Length);
            }
        }
    }
}