using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using AnubisWorks.Tools.Versioner.Entity;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Infrastructure.Services;
using AnubisWorks.Tools.Versioner.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner
{
    public class ProjectEntityMonoRepo
    {
        private readonly ILogger log;
        private readonly IProjectVersionCalculator _projectVersionCalculator;
        
        string _filePath { get; set; }

        public ProjectEntityMonoRepo(string filePath, string workingFolder, ref List<string> tcOutputs,
            string buildLabel,
            bool storeVersionFile, CustomProjectSettings customProjectSettings, VersionedSetModel calculatedModel)
        {
            this.log = Log.ForContext("Context", nameof(ProjectEntity), true);
            _filePath = filePath;

            // Inicjalizacja serwisów
            var gitLogService = new GitLogService(log);
            var versionPatternService = new VersionPatternService();
            var projectConfigurationService = new ProjectConfigurationService();
            var projectFileService = new ProjectFileService();
            
            _projectVersionCalculator = new ProjectVersionCalculatorService(
                log,
                gitLogService,
                versionPatternService,
                projectConfigurationService,
                projectFileService);

            // Użycie nowego serwisu do przetwarzania MonoRepo
            _projectVersionCalculator.ProcessMonoRepoProject(
                filePath,
                workingFolder,
                ref tcOutputs,
                buildLabel,
                storeVersionFile,
                customProjectSettings,
                calculatedModel);
        }
    }
}