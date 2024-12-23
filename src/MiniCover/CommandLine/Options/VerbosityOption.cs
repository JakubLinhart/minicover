﻿using System;
using Microsoft.Extensions.Logging;
using MiniCover.Exceptions;
using MiniCover.IO;

namespace MiniCover.CommandLine.Options
{
    public class VerbosityOption : ISingleValueOption, IVerbosityOption
    {
        private readonly IOutput _output;

        public VerbosityOption(IOutput output)
        {
            _output = output;
        }

        public string Name => "--verbosity";
        public string ShortName => "-v";
        public string Description => $"Change verbosity level ({GetPossibleValues()}) [default: {_output.MinimumLevel}]";

        private static string GetPossibleValues()
        {
            return string.Join(", ", new[] {
                LogLevel.Trace,
                LogLevel.Debug,
                LogLevel.Information,
                LogLevel.Warning,
                LogLevel.Error,
                LogLevel.Critical
            });
        }

        public void ReceiveValue(string value)
        {
            if (value != null)
            {
                if (!Enum.TryParse<LogLevel>(value, true, out var logLevel))
                    throw new ValidationException($"Invalid verbosity '{value}'");

                _output.MinimumLevel = logLevel;
            }
        }
    }
}