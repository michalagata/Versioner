using System;
using System.IO;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    public interface IConfigurationService
    {
        string LoadConfiguration(string configurationFile, IFileOperations fileOperations);
        string FillConfigWithDefaults(string json, ILogger log);
        void InitializeConfiguration(string configuration, ILogger log);
        ConfigurationValidationResult ValidateConfiguration(string configurationJson);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly ConfigurationValidator _validator;

        public ConfigurationService()
        {
            _validator = new ConfigurationValidator(Log.ForContext<ConfigurationService>());
        }

        public ConfigurationService(ILogger logger)
        {
            _validator = new ConfigurationValidator(logger ?? Log.ForContext<ConfigurationService>());
        }

        public string LoadConfiguration(string configurationFile, IFileOperations fileOperations)
        {
            if (!string.IsNullOrWhiteSpace(configurationFile))
            {
                if (!fileOperations.FileExists(configurationFile))
                {
                    throw new FileNotFoundException($"Configuration file not found: {configurationFile}");
                }
                return fileOperations.ReadFileContent(configurationFile);
            }
            return "{}";
        }

        public string FillConfigWithDefaults(string json, ILogger log)
        {
            JObject o = JObject.Parse(json);
            VersioningBaseConfiguration defEntry = new VersioningBaseConfiguration();
            if(o.ContainsKey("Default"))
            {
                defEntry = o["Default"].ToObject<VersioningBaseConfiguration>();
            }

            var def = JToken.FromObject(defEntry);
            try
            {
                if(o.ContainsKey("Items"))
                {
                    foreach(JToken item in o.SelectToken("Items").Children())
                    {
                        if(item["AssemblyInfoFile"] == null) item["AssemblyInfoFile"] = def["AssemblyInfoFile"];
                        if(item["AssemblyInfoVersionSet"] == null) item["AssemblyInfoVersionSet"] = def["AssemblyInfoVersionSet"];
                        if(item["AssemblyInfoVersionFormat"] == null) item["AssemblyInfoVersionFormat"] = def["AssemblyInfoVersionFormat"];
                        if(item["AssemblyVersionSet"] == null) item["AssemblyVersionSet"] = def["AssemblyVersionSet"];
                        if(item["AssemblyVersionFormat"] == null) item["AssemblyVersionFormat"] = def["AssemblyVersionFormat"];
                        if(item["AssemblyFileVersionSet"] == null) item["AssemblyFileVersionSet"] = def["AssemblyFileVersionSet"];
                        if(item["AssemblyFileVersionFormat"] == null) item["AssemblyFileVersionFormat"] = def["AssemblyFileVersionFormat"];
                        if(item["HashAsDescription"] == null) item["HashAsDescription"] = def["HashAsDescription"];
                    }
                }
            }
            catch(Exception e)
            {
                log.Error("Error filling with defaults.");
                Environment.Exit(701);
            }

            return o.ToString(Formatting.Indented);
        }

        public void InitializeConfiguration(string configuration, ILogger log)
        {
            State.TimeFrameConfiguration = new TimeFrameConfiguration() { TimeFrames = SemVerBasePreparator.ReturnSprintList() };
            var configStr = configuration ?? "{}";
            try
            {
                log.Information("Parsing commandline config");
                
                // Validate configuration before processing
                var validationResult = ValidateConfiguration(configStr);
                if (validationResult.HasErrors)
                {
                    log.Error("Configuration validation failed:");
                    foreach (var error in validationResult.Errors)
                    {
                        log.Error("  - {error}", error);
                    }
                    throw new InvalidOperationException("Configuration validation failed. See errors above.");
                }

                if (validationResult.HasWarnings)
                {
                    log.Warning("Configuration validation warnings:");
                    foreach (var warning in validationResult.Warnings)
                    {
                        log.Warning("  - {warning}", warning);
                    }
                }

                configStr = FillConfigWithDefaults(configStr, log);
                State.Config = JObject.Parse(configStr).ToObject<Configuration>();
            }
            catch (Exception e)
            {
                log.Error("Error deserializing config Use command line. {msg}", e.Message);
                Environment.Exit(500);
            }
        }

        public ConfigurationValidationResult ValidateConfiguration(string configurationJson)
        {
            return _validator.Validate(configurationJson);
        }
    }
} 