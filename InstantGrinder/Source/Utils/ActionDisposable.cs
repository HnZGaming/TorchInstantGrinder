using System;

namespace InstantGrinder.Utils
{
    public sealed class ActionDisposable : IDisposable
    {
        readonly Action _action;

        public ActionDisposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}