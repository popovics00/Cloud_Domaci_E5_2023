using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;

namespace Model
{
    [ServiceContract]
    public interface ITransactionCoordinator : IService
    {
        [OperationContract]
        Task<string> Buy(RequestForm form);
    }
}
