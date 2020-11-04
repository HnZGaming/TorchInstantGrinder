using System;
using System.Reflection;
using Sandbox.Game;
using VRage;
using VRage.Game;

namespace InstantGrinder.Reflections
{
    public static class MyInventoryAddItemsInternalReflection
    {
        const string MethodName = "AddItemsInternal";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        static readonly Type[] ParameterTypes =
        {
            typeof(MyFixedPoint),
            typeof(MyObjectBuilder_PhysicalObject),
            typeof(uint?),
            typeof(int),
        };

        static readonly MethodInfo Method = typeof(MyInventory).GetMethod(MethodName, Flags, null, ParameterTypes, null);
        static readonly object[] Args = new object[ParameterTypes.Length];

        public static void AddItemsInternal(
            this MyInventory self,
            MyObjectBuilder_PhysicalObject objectBuilder,
            MyFixedPoint amount)
        {
            lock (Args)
            {
                Args[0] = amount;
                Args[1] = objectBuilder;
                Args[2] = null;
                Args[3] = -1;
                Method.Invoke(self, Flags, null, Args, null);
                Array.Clear(Args, 0, Args.Length);
            }
        }
    }
}