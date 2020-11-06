using System.Collections.Generic;

namespace InstantGrinder
{
    public sealed class GrindByNameCommandOption
    {
        public const string Prefix = "--";
        public const string Key_Force = "force";

        public GrindByNameCommandOption(IEnumerable<string> rawOptions)
        {
            foreach (var rawOption in rawOptions)
            {
                if (!rawOption.StartsWith(Prefix)) continue;
                var optionKey = rawOption.Substring(2);
                switch (optionKey)
                {
                    case Key_Force:
                    {
                        Force = true;
                        continue;
                    }
                }
            }
        }

        public bool Force { get; }
    }
}