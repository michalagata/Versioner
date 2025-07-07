using System;
using System.Threading.Tasks;
using AnubisWorks.Tools.Versioner.Interfaces;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class Mediator : IMediator
    {
        private readonly AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider _serviceProvider;

        public Mediator(AnubisWorks.Tools.Versioner.Interfaces.IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> Send<TRequest, TResponse>(TRequest request) where TRequest : IRequest<TResponse>
        {
            var handlerType = typeof(IRequestHandler<TRequest, TResponse>);
            var handler = (IRequestHandler<TRequest, TResponse>)_serviceProvider.GetService(handlerType);
            return await handler.Handle(request);
        }
    }
} 