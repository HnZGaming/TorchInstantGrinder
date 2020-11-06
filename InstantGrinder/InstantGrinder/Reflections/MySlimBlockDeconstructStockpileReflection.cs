using System;
using System.Reflection;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;

namespace InstantGrinder.Reflections
{
    public static class MySlimBlockDeconstructStockpileReflection
    {
        const string MethodName = "DeconstructStockpile";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        static readonly Type[] ParameterTypes =
        {
            typeof(float),
            typeof(MyInventoryBase),
            typeof(bool),
        };

        static readonly MethodInfo Method = typeof(MySlimBlock).GetMethod(MethodName, Flags, null, ParameterTypes, null);
        static readonly object[] Args = new object[ParameterTypes.Length];

        public static void DeconstructStockpile(
            this MySlimBlock self,
            float deconstructAmount,
            MyInventoryBase outputInventory,
            bool useDefaultDeconstructEfficiency = false)
        {
            lock (Args)
            {
                Args[0] = deconstructAmount;
                Args[1] = outputInventory;
                Args[2] = useDefaultDeconstructEfficiency;
                Method.Invoke(self, Flags, null, Args, null);
                Array.Clear(Args, 0, Args.Length);
            }
        }
    }
}