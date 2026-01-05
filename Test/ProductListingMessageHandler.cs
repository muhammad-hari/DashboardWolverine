using Npgsql;
using System.Data;
using Wolverine;
using Wolverine.Attributes;

namespace Test
{
    [WolverineHandler]
    public class ProductListingMessageHandler
    {
        #region Private Members


        #endregion

        public ProductListingMessageHandler()
        {
        }


        [MoveToErrorQueueOn(typeof(NpgsqlException))]
        [RequeueOn(typeof(DBConcurrencyException), 1)]
        [MaximumAttempts(1)]
        public async Task HandleAsync(ProductListingEventRequest request, IMessageContext context, ILogger<ProductListingEventRequest> logger)
        {
            if(request.SyncData.Article == "999999999")
            {
                throw new DBConcurrencyException("Simulated concurrency exception for testing.");
            }

            Console.WriteLine("Product Listing Consumer");

           await Task.CompletedTask;
        }
    }
}
