using System;
using System.Collections.Generic;
using System.Linq;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Services.Strategies;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    /// <summary>
    /// Factory for creating and managing versioning strategy plugins.
    /// </summary>
    public interface IVersioningStrategyFactory
    {
        /// <summary>
        /// Gets a strategy by name.
        /// </summary>
        IVersioningStrategy GetStrategy(string strategyName);

        /// <summary>
        /// Gets the default strategy (Semantic Versioning).
        /// </summary>
        IVersioningStrategy GetDefaultStrategy();

        /// <summary>
        /// Gets all available strategies.
        /// </summary>
        IEnumerable<IVersioningStrategy> GetAllStrategies();

        /// <summary>
        /// Registers a custom strategy plugin.
        /// </summary>
        void RegisterStrategy(IVersioningStrategy strategy);
    }

    /// <summary>
    /// Factory implementation for versioning strategies.
    /// </summary>
    public class VersioningStrategyFactory : IVersioningStrategyFactory
    {
        private readonly Dictionary<string, IVersioningStrategy> _strategies;
        private readonly ILogger _logger;
        private readonly IGitLogService _gitLogService;

        public VersioningStrategyFactory(ILogger logger, IGitLogService gitLogService)
        {
            _logger = logger;
            _gitLogService = gitLogService;
            _strategies = new Dictionary<string, IVersioningStrategy>(StringComparer.OrdinalIgnoreCase);

            // Register built-in strategies
            RegisterBuiltInStrategies();
        }

        private void RegisterBuiltInStrategies()
        {
            // Register Semantic Versioning (default)
            var semanticStrategy = new Strategies.SemanticVersioningStrategy(_logger, _gitLogService);
            _strategies[semanticStrategy.Name] = semanticStrategy;

            // Register Calendar Versioning
            var calendarStrategy = new Strategies.CalendarVersioningStrategy(_logger, _gitLogService);
            _strategies[calendarStrategy.Name] = calendarStrategy;

            _logger.Debug("Registered {count} built-in versioning strategies", _strategies.Count);
        }

        public IVersioningStrategy GetStrategy(string strategyName)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                return GetDefaultStrategy();
            }

            if (_strategies.TryGetValue(strategyName, out var strategy))
            {
                return strategy;
            }

            _logger.Warning("Strategy '{strategy}' not found, using default", strategyName);
            return GetDefaultStrategy();
        }

        public IVersioningStrategy GetDefaultStrategy()
        {
            return _strategies["Semantic"];
        }

        public IEnumerable<IVersioningStrategy> GetAllStrategies()
        {
            return _strategies.Values;
        }

        public void RegisterStrategy(IVersioningStrategy strategy)
        {
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            if (string.IsNullOrWhiteSpace(strategy.Name))
            {
                throw new ArgumentException("Strategy name cannot be null or empty", nameof(strategy));
            }

            if (_strategies.ContainsKey(strategy.Name))
            {
                _logger.Warning("Strategy '{name}' already exists, replacing with new implementation", strategy.Name);
            }

            _strategies[strategy.Name] = strategy;
            _logger.Information("Registered custom versioning strategy: {name}", strategy.Name);
        }
    }
}

