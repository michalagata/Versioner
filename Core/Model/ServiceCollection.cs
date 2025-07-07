using System;
using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Interfaces;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class ServiceCollection : Dictionary<Type, Func<AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider, object>>, AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider
    {
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();

        public object GetService(Type serviceType)
        {
            if (TryGetValue(serviceType, out var factory))
            {
                if (_singletonInstances.TryGetValue(serviceType, out var instance))
                {
                    return instance;
                }

                var newInstance = factory(this);
                if (newInstance != null)
                {
                    _singletonInstances[serviceType] = newInstance;
                }
                return newInstance;
            }
            return null;
        }

        public AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider BuildServiceProvider()
        {
            return this;
        }

        public void AddSingleton<TService>(Func<AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider, TService> implementationFactory) where TService : class
        {
            this[typeof(TService)] = sp => implementationFactory(sp);
        }

        public void AddTransient<TService>(Func<AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider, TService> implementationFactory) where TService : class
        {
            this[typeof(TService)] = sp => implementationFactory(sp);
        }
    }
} 