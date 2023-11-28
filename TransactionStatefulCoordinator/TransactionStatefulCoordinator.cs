using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Model;
using static System.Reflection.Metadata.BlobBuilder;

namespace TransactionStatefulCoordinator
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class TransactionStatefulCoordinator : StatefulService, ITransactionCoordinator
    {
        public TransactionStatefulCoordinator(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<string> Buy(RequestForm form)
        {
            try
            {
                var stateManager = this.StateManager;

                var bookProxy = ServiceProxy.Create<IBookService>(new Uri("fabric:/Application1/BookStatefulService"), new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1));
                var bankProxy = ServiceProxy.Create<IBankService>(new Uri("fabric:/Application1/BankStatefulService"), new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1));

                var price = await bookProxy.CheckPrice(form.BookId, form.BookCount);
                
                if (price < 0)
                    return "There is no book with id: " + form.BookId;                
                else if (price == 0)
                    return "There is no enough book with id: " + form.BookId;
                
                var returnValue = await bankProxy.TakeMoney(form.AccountNumber, price);

                using var transaction = stateManager.CreateTransaction();

                var dictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, StatsItem>>("total");

                var total = await dictionary.TryGetValueAsync(transaction, "total").ConfigureAwait(false);

                if (returnValue.Item1 > 0)
                {
                    await bookProxy.GetBooks(form.BookId, form.BookCount);                    

                    total.Value.Sum = total.Value.Sum + price;

                }

                await transaction.CommitAsync();

                return $"OK: {total.Value.Sum}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task InitializeAsync()
        {
            int total = 0;

            var stateManager = this.StateManager;
            var dictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, StatsItem>>("total");

            using var transaction = stateManager.CreateTransaction();

            var item = new StatsItem { Sum = 0 };
            await dictionary.AddOrUpdateAsync(transaction, "total", item , (k, v) => item);

            await transaction.CommitAsync();
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners<TransactionStatefulCoordinator>();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync().ConfigureAwait(false);

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
