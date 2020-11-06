﻿namespace TorchUtils
{
    public class StringFloatConverter : AbsJsonConverter<float>
    {
        protected override float Parse(string str)
        {
            return float.Parse(str);
        }

        protected override string ReverseParse(float obj)
        {
            return $"{obj:0.00}";
        }
    }
}