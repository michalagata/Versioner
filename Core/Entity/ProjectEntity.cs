using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AnubisWorks.Tools.Versioner.Domain.Interfaces;
using AnubisWorks.Tools.Versioner.Entity;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Application.Commands;
using AnubisWorks.Tools.Versioner.Infrastructure.Services;
using AnubisWorks.Tools.Versioner.Interfaces;
using Newtonsoft.Json.Linq;
using Serilog;
using Formatting = Newtonsoft.Json.Formatting;

namespace AnubisWorks.Tools.Versioner
{
    public class ProjectEntity
    {
        private readonly ILogger log;
        private readonly IProjectVersionCalculator _projectVersionCalculator;
        
        string _filePath { get; set; }
        private VersionedSetModel VersionedSetReturner { get; set; }

        public ProjectEntity(string gitPath, string filePath, string workingFolder, ref List<string> consoleOutputs, string buildLabel,
            bool storeVersionFile, MajorMinorPatchHotfixModel patchHotfixModel,
            CustomProjectSettings customProjectSettings,
            string PrereleaseSuffix,
            string DefinedPatch,
            bool calculateMonoMode = false)
        {
            this.log = Log.ForContext("Context", nameof(ProjectEntity), true);
            _filePath = filePath;

            // Inicjalizacja serwisów
            var gitLogService = new GitLogService(log);
            var versionPatternService = new VersionPatternService();
            var projectConfigurationService = new ProjectConfigurationService();
            var projectFileService = new ProjectFileService();
            var versionPropertyInjector = new VersionPropertyInjector(log);
            
            _projectVersionCalculator = new ProjectVersionCalculatorService(
                log,
                gitLogService,
                versionPatternService,
                projectConfigurationService,
                projectFileService,
                versionPropertyInjector);

            // Użycie nowego serwisu do kalkulacji wersji (zawsze SemVer V1)
            VersionedSetReturner = _projectVersionCalculator.CalculateVersion(
                gitPath,
                filePath,
                workingFolder,
                ref consoleOutputs,
                buildLabel,
                storeVersionFile,
                patchHotfixModel,
                customProjectSettings,
                PrereleaseSuffix,
                DefinedPatch,
                calculateMonoMode);
        }

        public VersionedSetModel ReturnCalculatedModel()
        {
            if (VersionedSetReturner.IsSet)
            {
                log.Verbose("Returning calculated and operational MonoModel.");
                this.log.Verbose(
                    $"AssemblyFileVersion: {VersionedSetReturner.AssemblyFileVersion}, AssemblyInfoVersion: {VersionedSetReturner.AssemblyInfoVersion}, AssemblyVersion: {VersionedSetReturner.AssemblyVersion}, BuildLabel: {VersionedSetReturner.BuildLabel}, Description: {VersionedSetReturner.Description}, ShortHash: {VersionedSetReturner.ShortHash}.");
                return VersionedSetReturner;
            }

            {
                log.Verbose("Returning blank MonoModel!");
                return new VersionedSetModel();
            }
        }
    }
}