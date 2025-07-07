using System;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IServiceProvider
    {
        object GetService(Type serviceType);
    }
} 