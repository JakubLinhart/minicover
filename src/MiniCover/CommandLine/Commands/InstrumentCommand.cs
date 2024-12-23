﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniCover.CommandLine;
using MiniCover.CommandLine.Options;
using MiniCover.Core.Instrumentation;
using MiniCover.Core.Model;
using MiniCover.Exceptions;
using Newtonsoft.Json;

namespace MiniCover.Commands
{
    public class InstrumentCommand : ICommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly VerbosityOption _verbosityOption;
        private readonly WorkingDirectoryOption _workingDirectoryOption;
        private readonly ParentDirectoryOption _parentDirOption;
        private readonly IncludeAssembliesPatternOption _includeAssembliesOption;
        private readonly ExcludeAssembliesPatternOption _excludeAssembliesOption;
        private readonly IncludeSourcesPatternOption _includeSourceOption;
        private readonly ExcludeSourcesPatternOption _excludeSourceOption;
        private readonly IncludeTestsPatternOption _includeTestsOption;
        private readonly ExcludeTestsPatternOption _excludeTestsOption;
        private readonly HitsDirectoryOption _hitsDirectoryOption;
        private readonly CoverageFileOption _coverageFileOption;
        private readonly IInstrumenter _instrumenter;
        private readonly ILogger _logger;

        public InstrumentCommand(IServiceProvider serviceProvider,
            VerbosityOption verbosityOption,
            WorkingDirectoryOption workingDirectoryOption,
            ParentDirectoryOption parentDirOption,
            IncludeAssembliesPatternOption includeAssembliesOption,
            ExcludeAssembliesPatternOption excludeAssembliesOption,
            IncludeSourcesPatternOption includeSourceOption,
            ExcludeSourcesPatternOption excludeSourceOption,
            IncludeTestsPatternOption includeTestsOption,
            ExcludeTestsPatternOption excludeTestsOption,
            HitsDirectoryOption hitsDirectoryOption,
            CoverageFileOption coverageFileOption,
            IInstrumenter instrumenter,
            ILogger<InstrumentCommand> logger)
        {
            _serviceProvider = serviceProvider;
            _verbosityOption = verbosityOption;
            _workingDirectoryOption = workingDirectoryOption;
            _parentDirOption = parentDirOption;
            _includeAssembliesOption = includeAssembliesOption;
            _excludeAssembliesOption = excludeAssembliesOption;
            _includeSourceOption = includeSourceOption;
            _excludeSourceOption = excludeSourceOption;
            _includeTestsOption = includeTestsOption;
            _excludeTestsOption = excludeTestsOption;
            _hitsDirectoryOption = hitsDirectoryOption;
            _coverageFileOption = coverageFileOption;
            _instrumenter = instrumenter;
            _logger = logger;
        }

        public string CommandName => "instrument";
        public string CommandDescription => "Instrument assemblies";
        public IOption[] Options => new IOption[]
        {
            _verbosityOption,
            _workingDirectoryOption,
            _parentDirOption,
            _includeAssembliesOption,
            _excludeAssembliesOption,
            _includeSourceOption,
            _excludeSourceOption,
            _includeTestsOption,
            _excludeTestsOption,
            _hitsDirectoryOption,
            _coverageFileOption
        };

        public Task<int> Execute()
        {
            var discoveryWatch = Stopwatch.StartNew();
            var assemblies = GetFiles(_includeAssembliesOption.Value, _excludeAssembliesOption.Value, _parentDirOption.DirectoryInfo);
            if (assemblies.Length == 0)
                throw new ValidationException("No assemblies found");
            _logger.LogInformation("Found {assembliesCount} assemblies.", assemblies.Length);

            var sourceFiles = GetFiles(_includeSourceOption.Value, _excludeSourceOption.Value, _parentDirOption.DirectoryInfo);
            if (sourceFiles.Length == 0)
                throw new ValidationException("No source files found");
            _logger.LogInformation("Found {sourceFilesCount} source files.", sourceFiles.Length);

            var testFiles = GetFiles(_includeTestsOption.Value, _excludeTestsOption.Value, _parentDirOption.DirectoryInfo);
            _logger.LogInformation("Found {testFilesCount} test files.", testFiles.Length);
            discoveryWatch.Stop();
            _logger.LogInformation("Discovery done in {discoveryTime} ms.", discoveryWatch.ElapsedMilliseconds);

            var instrumentationContext = new FileBasedInstrumentationContext
            {
                Assemblies = assemblies,
                HitsPath = _hitsDirectoryOption.DirectoryInfo.FullName,
                Sources = sourceFiles,
                Tests = testFiles,
                Workdir = _workingDirectoryOption.DirectoryInfo
            };

            var instrumentationWatch = Stopwatch.StartNew();
            var result = _instrumenter.Instrument(instrumentationContext);
            instrumentationWatch.Stop();
            _logger.LogInformation("Instrumentation done in {instrumentationTime} ms.", instrumentationWatch.ElapsedMilliseconds);

            _logger.LogInformation("Writing coverage file {coverageFile}", _coverageFileOption.FileInfo.FullName);
            var coverageFile = _coverageFileOption.FileInfo;
            SaveCoverageFile(coverageFile, result);

            return Task.FromResult(0);
        }

        private static IFileInfo[] GetFiles(
            IEnumerable<string> includes,
            IEnumerable<string> excludes,
            IDirectoryInfo parentDir)
        {
            var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();

            foreach (var include in includes)
            {
                matcher.AddInclude(include);
            }

            foreach (var exclude in excludes)
            {
                matcher.AddExclude(exclude);
            }

            var fileMatchResult = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(parentDir.FullName)));

            return fileMatchResult.Files
                .Select(f => parentDir.FileSystem.FileInfo.New(Path.GetFullPath(Path.Combine(parentDir.ToString(), f.Path))))
                .ToArray();
        }

        private static void SaveCoverageFile(IFileInfo coverageFile, InstrumentationResult result)
        {
            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            var json = JsonConvert.SerializeObject(result, Formatting.Indented, settings);
            File.WriteAllText(coverageFile.FullName, json);
        }
    }
}
