using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Model;

namespace BankStatefulService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class BankStatefulService : StatefulService, IBankService
    {
        public BankStatefulService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task InitializeAsync()
        {
            List<BankAccount> accounts = new()
            {
                new BankAccount() { AccountNumber = 1, AmountOfMoney = 15000 },
                new BankAccount() { AccountNumber = 2, AmountOfMoney = 1000 },
                new BankAccount() { AccountNumber = 3, AmountOfMoney = 4700 },
                new BankAccount() { AccountNumber = 4, AmountOfMoney = 2000 },
                new BankAccount() { AccountNumber = 5, AmountOfMoney = 100 },
                new BankAccount() { AccountNumber = 6, AmountOfMoney = 1200 },
                new BankAccount() { AccountNumber = 7, AmountOfMoney = 9200 },
            };

            var stateManager = this.StateManager;
            var bankAccountDict = await stateManager.GetOrAddAsync<IReliableDictionary<long, BankAccount>>("bankAccountDict");

            using var transaction = stateManager.CreateTransaction();
            foreach (BankAccount account in accounts)
                await bankAccountDict.AddOrUpdateAsync(transaction, account.AccountNumber, account, (k, v) => account);

            await transaction.CommitAsync();
        }

        public async Task<Tuple<int, string>> TakeMoney(int accountId, double amount)
        {
            var stateManager = this.StateManager;
            var bankAccountDict = await stateManager.GetOrAddAsync<IReliableDictionary<long, BankAccount>>("bankAccountDict");

            using var transaction = stateManager.CreateTransaction();
            var account = await bankAccountDict.TryGetValueAsync(transaction, accountId);

            if (!account.HasValue)
                return new Tuple<int, string>(-1, "There is no bank account with id:" + accountId);

            if (account.Value.AmountOfMoney < amount)
                return new Tuple<int, string>(0, "There is no enough money at bank account");

            account.Value.AmountOfMoney -= amount;
            await transaction.CommitAsync();

            return new Tuple<int, string>(1, "Successfully bought book");
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
            return this.CreateServiceRemotingReplicaListeners<BankStatefulService>();
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
