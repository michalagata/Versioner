using System.Threading.Tasks;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IMediator
    {
        Task<TResponse> Send<TRequest, TResponse>(TRequest request) where TRequest : IRequest<TResponse>;
    }

    public interface IRequest<TResponse>
    {
    }

    public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request);
    }
} 