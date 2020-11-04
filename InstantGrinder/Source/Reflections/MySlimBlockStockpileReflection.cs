using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;

namespace InstantGrinder.Reflections
{
    public static class MySlimBlockStockpileReflection
    {
        const string FieldName = "m_stockpile";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo Field = typeof(MySlimBlock).GetField(FieldName, Flags);

        public static MyConstructionStockpile GetStockpile(this MySlimBlock self)
        {
            return (MyConstructionStockpile) Field.GetValue(self);
        }
    }
}