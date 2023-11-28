using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Model;

namespace TransactionCoordinator
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TransactionCoordinator : StatelessService, ITransactionCoordinator
    {
        public TransactionCoordinator(StatelessServiceContext context)
            : base(context)
        { }

        public async Task<string> Buy(RequestForm form)
        {
            if (form == null)
                return "TC: Form is empty!";

            else if (string.IsNullOrEmpty(form.FirstName) || string.IsNullOrEmpty(form.LastName) || form.BookCount == 0 || form.AccountNumber == 0 || form.BookId == 0)
                return "TC: Some fields are missing in the form";

            try
            {
                var bookProxy = ServiceProxy.Create<IBookService>(new Uri("fabric:/Application1/BookService"), new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1));
                var bankProxy = ServiceProxy.Create<IBankService>(new Uri("fabric:/Application1/BankService"), new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1));

                var price = await bookProxy.CheckPrice(form.BookId, form.BookCount);
                if (price < 0)
                {
                    return "There is no book with id: " + form.BookId;
                }

                if (price == 0)
                {
                    return "There is no enough book with id: " + form.BookId;
                }

                var returnValue = await bankProxy.TakeMoney(form.AccountNumber, price);

                if (returnValue.Item1 > 0)
                {
                    await bookProxy.GetBooks(form.BookId, form.BookCount);
                }

                return returnValue.Item2;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
