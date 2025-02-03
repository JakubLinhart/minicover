using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using Microsoft.Extensions.Logging;
using MiniCover.HitServices;

namespace MiniCover.Core.Hits
{
    public class HitsReader : IHitsReader
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<HitsReader> _logger;

        public HitsReader(IFileSystem fileSystem, ILogger<HitsReader> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public HitsInfo TryReadFromDirectory(string path)
        {
            var contexts = new List<HitContext>();

            if (_fileSystem.Directory.Exists(path))
            {
                foreach (var hitFile in _fileSystem.Directory.GetFiles(path, "*.hits"))
                {
                    using (var fileStream = OpenWithRetry(hitFile, 5, 100))
                    {
                        contexts.AddRange(HitContext.Deserialize(fileStream));
                    }
                }
            }

            return new HitsInfo(contexts);
        }

        private Stream OpenWithRetry(string fileName, int maxRetries, int backoffDelayMs)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    return _fileSystem.File.Open(fileName, FileMode.Open, FileAccess.Read);
                }
                catch (IOException)
                {
                    retryCount++;
                    if (retryCount > maxRetries)
                    {
                        throw; // Re-throw the exception if the maximum number of retries is reached
                    }
                    // Exponential backoff: 2^retryCount * 100 milliseconds
                    var delay = (int)Math.Pow(2, retryCount) * backoffDelayMs;
                    _logger.LogWarning($"Failed to open file {fileName}, retrying in {delay} ms.");
                    Thread.Sleep(delay);
                }
            }
        }
    }
}
