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
    public interface IBookService : IService
    {
        [OperationContract]
        Task<double> CheckPrice(int bookId, int bookCount);
        [OperationContract]
        Task<string> GetBooks(int bookId, int bookCount);
    }
}
