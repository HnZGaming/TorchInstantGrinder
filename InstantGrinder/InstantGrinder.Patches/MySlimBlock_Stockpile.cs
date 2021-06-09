using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;

namespace InstantGrinder.Patches
{
    public static class MySlimBlock_Stockpile
    {
        const string FieldName = "m_stockpile";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo Field = typeof(MySlimBlock).GetField(FieldName, Flags);

        public static MyConstructionStockpile Value(this MySlimBlock self)
        {
            return (MyConstructionStockpile) Field.GetValue(self);
        }
    }
}