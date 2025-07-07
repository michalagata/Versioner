using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnubisWorks.Tools.Versioner.Services;
using AnubisWorks.Tools.Versioner.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Model
{
    public static class VersionerFactory
    {
        public static IGitOperations CreateGitOperations(ILogger logger)
        {
            return new GitOperations(logger);
        }

        public static IFileOperations CreateFileOperations(ILogger logger)
        {
            return new FileOperations(logger);
        }

        public static IVersioningOperations CreateVersioningOperations(ILogger logger)
        {
            return new VersioningOperations(logger);
        }

        public static IConfigurationService CreateConfigurationService()
        {
            return new ConfigurationService();
        }

        public static AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            
            // Register services
            services.AddSingleton<IGitOperations>(sp => CreateGitOperations(Log.ForContext<PrimalPerformer>()));
            services.AddSingleton<IFileOperations>(sp => CreateFileOperations(Log.ForContext<PrimalPerformer>()));
            services.AddSingleton<IVersioningOperations>(sp => CreateVersioningOperations(Log.ForContext<PrimalPerformer>()));
            services.AddSingleton<IMediator>(sp => new Mediator(sp));
            services.AddSingleton<IDockerVersioningService>(sp => CreateDockerVersioningService(
                Log.ForContext<PrimalPerformer>(),
                sp.GetService(typeof(IGitOperations)) as IGitOperations,
                sp.GetService(typeof(IFileOperations)) as IFileOperations
            ));
            services.AddSingleton<IOverrideVersioningService>(sp => CreateOverrideVersioningService(
                Log.ForContext<PrimalPerformer>(),
                sp.GetService(typeof(IFileOperations)) as IFileOperations
            ));
            services.AddSingleton<IConfigurationService>(sp => CreateConfigurationService());
            
            // Register handlers
            services.AddTransient<IRequestHandler<VersioningRequest, VersioningResponse>>(sp => new VersioningRequestHandler(
                sp.GetService(typeof(IGitOperations)) as IGitOperations,
                sp.GetService(typeof(IFileOperations)) as IFileOperations,
                sp.GetService(typeof(IVersioningOperations)) as IVersioningOperations,
                Log.ForContext<VersioningRequestHandler>()
            ));
            
            return services.BuildServiceProvider();
        }

        public static IDockerVersioningService CreateDockerVersioningService(ILogger logger, IGitOperations gitOperations, IFileOperations fileOperations)
        {
            return new DockerVersioningService(logger, gitOperations, fileOperations);
        }

        public static IOverrideVersioningService CreateOverrideVersioningService(ILogger logger, IFileOperations fileOperations)
        {
            return new OverrideVersioningService(logger, fileOperations);
        }

        public static PrimalPerformer CreatePrimalPerformer(ConfigurationsArgs config, ILogger logger)
        {
            var gitOperations = CreateGitOperations(logger);
            var fileOperations = CreateFileOperations(logger);
            var versioningOperations = CreateVersioningOperations(logger);
            var dockerVersioningService = CreateDockerVersioningService(logger, gitOperations, fileOperations);
            var overrideVersioningService = CreateOverrideVersioningService(logger, fileOperations);
            var configurationService = CreateConfigurationService();

            return new PrimalPerformer(
                config,
                gitOperations,
                fileOperations,
                versioningOperations,
                new Mediator(CreateServiceProvider())
            );
        }
    }
} 