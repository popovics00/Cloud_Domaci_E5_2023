using Model;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;


namespace ValidateService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class ValidateService : StatelessService, IValidator
    {
        public ValidateService(StatelessServiceContext context)
            : base(context)
        { }

        public async Task<string> Validate(RequestForm form)
        {
            if (form == null)
                return "Form is empty!";

            else if (string.IsNullOrEmpty(form.FirstName) || string.IsNullOrEmpty(form.LastName) || form.BookCount == 0 || form.AccountNumber == 0 || form.BookId == 0)
                return "Some fields are missing in the form";


            try
            {
                var proxy = ServiceProxy.Create<ITransactionCoordinator>(new Uri("fabric:/Application1/TransactionStatefulCoordinator"), new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1));

                return await proxy.Buy(form);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
