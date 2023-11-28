using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;

namespace Model
{
    [ServiceContract]
    public interface IValidator : IService
    {
        [OperationContract]
        Task<string> Validate(RequestForm form);
    }
}
