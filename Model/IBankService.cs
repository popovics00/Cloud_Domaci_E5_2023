using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [ServiceContract]
    public interface IBankService : IService
    {
        [OperationContract]
        Task<Tuple<int, string>> TakeMoney(int accountId, double amount);
    }
}
