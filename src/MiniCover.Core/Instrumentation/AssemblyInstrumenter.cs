﻿using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniCover.Core.Extensions;
using MiniCover.Core.Model;
using MiniCover.Core.Utils;
using MiniCover.HitServices;
using Mono.Cecil;

namespace MiniCover.Core.Instrumentation
{
    public class AssemblyInstrumenter : IAssemblyInstrumenter
    {
        private static readonly ConstructorInfo instrumentedAttributeConstructor = typeof(InstrumentedAttribute).GetConstructors().First();

        private readonly ITypeInstrumenter _typeInstrumenter;
        private readonly IFileSystem _fileSystem;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AssemblyInstrumenter> _logger;

        public AssemblyInstrumenter(
            ITypeInstrumenter typeInstrumenter,
            IFileSystem fileSystem,
            IServiceProvider serviceProvider,
            ILogger<AssemblyInstrumenter> logger)
        {
            _typeInstrumenter = typeInstrumenter;
            _fileSystem = fileSystem;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public InstrumentedAssembly InstrumentAssemblyFile(
            IInstrumentationContext context,
            IFileInfo assemblyFile)
        {
            var assemblyDirectory = assemblyFile.Directory;

            var resolver = ActivatorUtilities.CreateInstance<CustomAssemblyResolver>(_serviceProvider, assemblyDirectory);

            _logger.LogTrace("Assembly resolver search directories: {directories}", [resolver.GetSearchDirectories()]);

            try
            {
                using var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile.FullName, new ReaderParameters { ReadSymbols = true, AssemblyResolver = resolver });
                return InstrumentAssemblyDefinition(context, assemblyDefinition);
            }
            catch (BadImageFormatException)
            {
                _logger.LogInformation("Invalid assembly format");
                return null;
            }
        }

        private InstrumentedAssembly InstrumentAssemblyDefinition(
            IInstrumentationContext context,
            AssemblyDefinition assemblyDefinition)
        {
            if (assemblyDefinition.CustomAttributes.Any(a => a.AttributeType.Name == "InstrumentedAttribute"))
            {
                _logger.LogInformation("Already instrumented");
                return null;
            }

            var assemblyDocuments = assemblyDefinition.GetAllDocuments();

            var changedDocuments = assemblyDocuments.Where(d => d.FileHasChanged()).ToArray();
            if (changedDocuments.Length != 0)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var changedFiles = changedDocuments.Select(d => d.Url).Distinct().ToArray();
                    _logger.LogDebug("Source files has changed: {changedFiles}", [changedFiles]);
                }
                else
                {
                    _logger.LogInformation("Source files has changed");
                }
                return null;
            }

            var instrumentedAssembly = new InstrumentedAssembly(assemblyDefinition.Name.Name);
            var instrumentedAttributeReference = assemblyDefinition.MainModule.ImportReference(instrumentedAttributeConstructor);
            assemblyDefinition.CustomAttributes.Add(new CustomAttribute(instrumentedAttributeReference));

            foreach (var typeDefinition in assemblyDefinition.MainModule.GetTypes())
            {
                if (typeDefinition.FullName == "<Module>"
                    || typeDefinition.FullName == "AutoGeneratedProgram"
                    || typeDefinition.DeclaringType != null)
                    continue;

                _typeInstrumenter.InstrumentType(
                    context,
                    typeDefinition,
                    instrumentedAssembly);
            }

            if (!instrumentedAssembly.Methods.Any()) {
                _logger.LogInformation("Nothing to instrument");
                return null;
            }

            _logger.LogInformation("Assembly instrumented");

            var miniCoverTempPath = GetMiniCoverTempPath();

            var instrumentedAssemblyFile = _fileSystem.FileInfo.New(Path.Combine(miniCoverTempPath, $"{Guid.NewGuid()}.dll"));
            var instrumentedPdbFile = FileUtils.GetPdbFile(instrumentedAssemblyFile);

            assemblyDefinition.Write(instrumentedAssemblyFile.FullName, new WriterParameters { WriteSymbols = true });

            instrumentedAssembly.TempAssemblyFile = instrumentedAssemblyFile.FullName;
            instrumentedAssembly.TempPdbFile = instrumentedPdbFile.FullName;

            return instrumentedAssembly;
        }

        private string GetMiniCoverTempPath()
        {
            var path = Path.Combine(Path.GetTempPath(), "minicover");
            if (!_fileSystem.Directory.Exists(path))
                _fileSystem.Directory.CreateDirectory(path);
            return path;
        }
    }
}
