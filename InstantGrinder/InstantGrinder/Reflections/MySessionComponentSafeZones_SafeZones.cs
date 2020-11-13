using System.Reflection;
using Sandbox.Game.Entities;
using VRage.Collections;

namespace InstantGrinder.Reflections
{
    public sealed class MySessionComponentSafeZones_SafeZones
    {
        const string FieldName = "m_safeZones";
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        static readonly FieldInfo FieldInfo = typeof(MySessionComponentSafeZones).GetField(FieldName, Flags);

        public static MyConcurrentList<MySafeZone> Field => (MyConcurrentList<MySafeZone>) FieldInfo.GetValue(null);
    }
}