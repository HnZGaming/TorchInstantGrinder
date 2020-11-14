using System.Collections.Generic;
using TorchUtils;

namespace InstantGrinder
{
    public sealed class GrindByNameCommandOption
    {
        const string Key_Force = "force";
        const string Key_PromoteLevelNone = "as_player";

        public GrindByNameCommandOption(IEnumerable<string> rawOptions)
        {
            foreach (var rawOption in rawOptions)
            {
                if (!CommandOption.TryGetOption(rawOption, out var option)) continue;

                if (option.IsParameterless(Key_Force))
                {
                    Force = true;
                }
                else if (option.IsParameterless(Key_PromoteLevelNone))
                {
                    AsPlayer = true;
                }
            }
        }

        public static string ForceOption => $"{CommandOption.Prefix}{Key_Force}";
        public bool Force { get; }
        public bool AsPlayer { get; }
    }
}