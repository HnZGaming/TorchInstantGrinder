using System.Collections.Generic;
using System.Reflection;
using Sandbox.Game.Entities;

namespace InstantGrinder.Patches
{
    public static class MySessionComponentSafeZones_SafeZones
    {
        const string FieldName = "m_safeZones";
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        static readonly FieldInfo Field = typeof(MySessionComponentSafeZones).GetField(FieldName, Flags);

        public static IEnumerable<MySafeZone> Value => (IEnumerable<MySafeZone>) Field.GetValue(null);
    }
}