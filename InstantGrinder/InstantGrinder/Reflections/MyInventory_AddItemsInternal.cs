using System;
using System.Reflection;
using Sandbox.Game;
using VRage;
using VRage.Game;

namespace InstantGrinder.Reflections
{
    public static class MyInventory_AddItemsInternal
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

        delegate void MethodDelegate(MyInventory self, MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, uint? itemId, int index);
        static readonly MethodInfo Method = typeof(MyInventory).GetMethod(MethodName, Flags, null, ParameterTypes, null);
        static readonly MethodDelegate MethodDelegateInstance = (MethodDelegate) Delegate.CreateDelegate(typeof(MethodDelegate), Method);

        public static void AddItemsInternal(
            this MyInventory self,
            MyObjectBuilder_PhysicalObject objectBuilder,
            MyFixedPoint amount)
        {
            MethodDelegateInstance(self, amount, objectBuilder, null, -1);
        }
    }
}