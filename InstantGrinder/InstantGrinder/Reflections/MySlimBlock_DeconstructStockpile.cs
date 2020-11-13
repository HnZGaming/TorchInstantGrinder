using System;
using System.Reflection;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;

namespace InstantGrinder.Reflections
{
    public static class MySlimBlock_DeconstructStockpile
    {
        const string MethodName = "DeconstructStockpile";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        static readonly Type[] ParameterTypes =
        {
            typeof(float),
            typeof(MyInventoryBase),
            typeof(bool),
        };

        delegate void MethodDelegate(MySlimBlock self, float deconstructAmount, MyInventoryBase outputInventory, bool useDefaultDeconstructEfficiency);
        static readonly MethodInfo Method = typeof(MySlimBlock).GetMethod(MethodName, Flags, null, ParameterTypes, null);
        static readonly MethodDelegate MethodDelegateInstance = (MethodDelegate) Delegate.CreateDelegate(typeof(MethodDelegate), Method);

        public static void DeconstructStockpile(
            this MySlimBlock self,
            float deconstructAmount,
            MyInventoryBase outputInventory,
            bool useDefaultDeconstructEfficiency = false)
        {
            MethodDelegateInstance(self, deconstructAmount, outputInventory, useDefaultDeconstructEfficiency);
        }
    }
}