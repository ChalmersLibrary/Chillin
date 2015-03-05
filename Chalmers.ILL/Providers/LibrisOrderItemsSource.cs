using Chalmers.ILL.Logging;
using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Patron;
using Chalmers.ILL.UmbracoApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace Chalmers.ILL.Providers
{
    public class LibrisOrderItemsSource : ISource
    {
        IUmbracoWrapper _umbraco;
        IOrderItemManager _orderItemManager;
        IInternalDbLogger _internalDbLogger;

        private List<OrderItemSeedModel> _seeds;
        private SourcePollingResult _result;
        public SourcePollingResult Result
        {
            get
            {
                return _result;
            }
        }

        public LibrisOrderItemsSource(IUmbracoWrapper umbraco, IOrderItemManager orderItemManager, IInternalDbLogger internalDbLogger)
        {
            _umbraco = umbraco;
            _orderItemManager = orderItemManager;
            _internalDbLogger = internalDbLogger;
        }

        public SourcePollingResult Poll()
        {
            _result = new SourcePollingResult("Libris");
            _seeds = new List<OrderItemSeedModel>();

            AddSeedsForNewUserRequestsFromLibris();

            AddPatronInfoToSeeds();

            CreateNewOrdersFromSeeds();

            return Result;
        }

        #region Private methods

        private void AddSeedsForNewUserRequestsFromLibris()
        {
            try
            {
                var addressStr = ConfigurationManager.AppSettings["librisApiBaseAddress"] + "/illse/api/userrequests/" + ConfigurationManager.AppSettings["librarySigel"];

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("api-key", ConfigurationManager.AppSettings["librisApiKey"]);
                var task = httpClient.GetStringAsync(new Uri(addressStr));

                task.Wait();

                var userRequestsQueryResult = JsonConvert.DeserializeObject<dynamic>(task.Result);

                var userRequests = userRequestsQueryResult.user_requests;
                for (int index = 0; index < userRequestsQueryResult.count.Value; index++)
                {
                    try
                    {
                        var req = userRequests[index];
                        var seed = new OrderItemSeedModel();
                        seed.PatronEmail = req.user.email.Value;
                        seed.PatronName = req.user.full_name.Value;
                        seed.PatronCardNumber = req.user.library_card.Value;
                        seed.Message = "LIBRIS LÅNTAGARBESTÄLLNING" + "\n\n" +
                            ConfigurationManager.AppSettings["librisApiBaseAddress"] + ConfigurationManager.AppSettings["librisApiUserRequestSuffix"] + "\n\n" +
                            "Författare: " + ReplaceWithNotAvailableIfEmptyString(req.author.Value) + "\n" +
                            "Titel: " + ReplaceWithNotAvailableIfEmptyString(req.title.Value) + "\n" +
                            "Utgivning: " + ReplaceWithNotAvailableIfEmptyString(req.imprint.Value) + "\n" +
                            "ISBN/ISSN: " + ReplaceWithNotAvailableIfEmptyString(req.isxn.Value);
                        _seeds.Add(seed);
                    }
                    catch (Exception e)
                    {
                        var msg = "Error when trying to add seed for a new user request. ";
                        _result.Errors++;
                        _result.Messages.Add(msg + e.Message);
                        _umbraco.LogError<LibrisOrderItemsSource>(msg, e);
                    }
                }
            }
            catch (Exception e)
            {
                throw new SourcePollingException("Error when trying to add seeds for new user requests.", e);
            }
        }

        private void AddPatronInfoToSeeds()
        {
            try
            {
                // TODO: Inject Sierra resources instead.
                using (Sierra s = new Sierra())
                {
                    s.Connect(ConfigurationManager.AppSettings["sierraConnectionString"]);

                    foreach (var seed in _seeds)
                    {
                        seed.SierraPatronInfo = s.GetPatronInfoFromLibraryCardNumberOrPersonnummer(seed.PatronCardNumber, seed.PatronCardNumber);
                    }
                }
            }
            catch (Exception e)
            {
                throw new SourcePollingException("Error connecting to Sierra.", e);
            }
        }

        private void CreateNewOrdersFromSeeds()
        {
            try
            {
                foreach (var seed in _seeds)
                {
                    try
                    {
                        int orderItemNodeId = _orderItemManager.CreateOrderItemInDbFromOrderItemSeedModel(seed, false, false);

                        _internalDbLogger.WriteSierraDataToLog(orderItemNodeId, seed.SierraPatronInfo);

                        _result.NewOrders++;
                    }
                    catch (Exception e)
                    {
                        var msg = "Error creating new OrderItem node. ";
                        _umbraco.LogError<LibrisOrderItemsSource>(msg, e);
                        _result.Errors++;
                        _result.Messages.Add(msg + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                throw new SourcePollingException("Error when creating new orders from seeds.", e);
            }
        }

        private string ReplaceWithNotAvailableIfEmptyString(string val)
        {
            return (val == "" ? "N/A" : val);
        }

        #endregion
    }
}