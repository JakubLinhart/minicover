﻿using System.IO.Abstractions;

namespace MiniCover.CommandLine.Options
{
    public abstract class DirectoryOption : ISingleValueOption, IDirectoryOption
    {
        private readonly IFileSystem _fileSystem;

        protected DirectoryOption(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IDirectoryInfo DirectoryInfo { get; private set; }
        public abstract string Name { get; }
        public string ShortName => null;
        public abstract string Description { get; }
        protected abstract string DefaultValue { get; }

        public virtual void ReceiveValue(string value)
        {
            DirectoryInfo = _fileSystem.DirectoryInfo.New(value ?? DefaultValue);
        }
    }
}