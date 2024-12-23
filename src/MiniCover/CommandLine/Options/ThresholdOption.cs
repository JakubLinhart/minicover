﻿using System.Globalization;

namespace MiniCover.CommandLine.Options
{
    public class ThresholdOption : ISingleValueOption, IThresholdOption
    {
        private const float _defaultValue = 90;

        public float Value { get; private set; }
        public string Name => "--threshold";
        public string Description => $"Coverage percentage threshold [default: {_defaultValue}]";

        public void ReceiveValue(string value)
        {
            if (!float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var threshold))
            {
                threshold = _defaultValue;
            }

            Value = threshold / 100;
        }
    }
}
