using System;
using System.Reflection;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRageMath;

namespace InstantGrinder.Patches
{
    public static class MySafeZone_IsOutside
    {
        delegate bool MethodDelegate(MySafeZone self, BoundingBoxD entity);

        const string MethodName = "IsOutside";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        static readonly Type[] ParameterTypes =
        {
            typeof(BoundingBoxD),
        };

        static readonly MethodInfo Method = typeof(MySafeZone).GetMethod(MethodName, Flags, null, ParameterTypes, null);
        static readonly MethodDelegate MethodDelegateInstance = (MethodDelegate) Delegate.CreateDelegate(typeof(MethodDelegate), Method);

        public static bool IsOutside(this MySafeZone self, BoundingBoxD entity)
        {
            return MethodDelegateInstance(self, entity);
        }
    }
}