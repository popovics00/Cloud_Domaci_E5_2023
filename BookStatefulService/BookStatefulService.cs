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

namespace BookStatefulService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class BookStatefulService : StatefulService, IBookService
    {
        public BookStatefulService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<double> CheckPrice(int bookId, int bookCount)
        {
            var stateManager = this.StateManager;
            var bookDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<long, Book>>("bookDictionary");

            using var transaction = stateManager.CreateTransaction();
            var book = await bookDictionary.TryGetValueAsync(transaction, bookId);

            if (!book.HasValue)
                return -1;
            else if (book.Value.Quantity >= bookCount)
                return book.Value.Price * bookCount;
            else
                return 0;

        }

        public async Task<string> GetBooks(int bookId, int bookCount)
        {
            var stateManager = this.StateManager;
            var bookDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<long, Book>>("bookDictionary");

            using var transaction = stateManager.CreateTransaction();
            var book = await bookDictionary.TryGetValueAsync(transaction, bookId);

            book.Value.Quantity -= bookCount;

            await bookDictionary.AddOrUpdateAsync(transaction, book.Value.Id, book.Value, (k, v) => book.Value);
            await transaction.CommitAsync();
            return "Successful buy!";
        }

        public async Task InitializeAsync()
        {
            List<Book> books = new()
            {
                new Book() { Id = 1, Title = "The Silent Stars Go By", Author = "George Orwell", Quantity = 15, Price = 1200 },
                new Book() { Id = 2, Title = "To Kill a Mockingbird", Author = "Harper Lee", Quantity = 10, Price = 800 },
                new Book() { Id = 3, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Quantity = 5, Price = 1500 },
                new Book() { Id = 4, Title = "1984", Author = "George Orwell", Quantity = 8, Price = 1000 },
                new Book() { Id = 5, Title = "Brave New World", Author = "Aldous Huxley", Quantity = 12, Price = 1800 },
                new Book() { Id = 6, Title = "The Catcher in the Rye", Author = "J.D. Salinger", Quantity = 7, Price = 950 },
                new Book() { Id = 7, Title = "The Hobbit", Author = "J.R.R. Tolkien", Quantity = 20, Price = 2000 },
                new Book() { Id = 8, Title = "One Hundred Years of Solitude", Author = "Gabriel Garcia Marquez", Quantity = 3, Price = 700 },
                new Book() { Id = 9, Title = "The Lord of the Rings", Author = "J.R.R. Tolkien", Quantity = 18, Price = 2200 },
                new Book() { Id = 10, Title = "The Alchemist", Author = "Paulo Coelho", Quantity = 14, Price = 1300 }

            };

            var stateManager = this.StateManager;
            var bookDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<long, Book>>("bookDictionary");

            using var transaction = stateManager.CreateTransaction();
            foreach (Book book in books)
                await bookDictionary.AddOrUpdateAsync(transaction, book.Id, book, (k, v) => book);

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
            return this.CreateServiceRemotingReplicaListeners<BookStatefulService>();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();

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
