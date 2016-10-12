﻿using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Patron;
using Chalmers.ILL.UmbracoApi;
using Chalmers.ILL.Utilities;
using Examine;
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
        public static int CREATE_ORDER_FROM_LIBRIS_DATA_EVENT_TYPE { get { return 17; } }
        public static int UPDATE_ORDER_FROM_LIBRIS_DATA_EVENT_TYPE { get { return 18; } }

        IUmbracoWrapper _umbraco;
        IOrderItemManager _orderItemManager;
        IPatronDataProvider _patronDataProvider;

        private List<OrderItemSeedModel> _seeds;
        private SourcePollingResult _result;
        public SourcePollingResult Result
        {
            get
            {
                return _result;
            }
        }

        public LibrisOrderItemsSource(IUmbracoWrapper umbraco, IOrderItemManager orderItemManager, IPatronDataProvider patronDataProvider)
        {
            _umbraco = umbraco;
            _orderItemManager = orderItemManager;
            _patronDataProvider = patronDataProvider;
        }

        public SourcePollingResult Poll()
        {
            _result = new SourcePollingResult("Libris");
            _seeds = new List<OrderItemSeedModel>();

            AddSeedsForNewUserRequestsFromLibris();

            AddPatronInfoToSeeds();

            CreateNewOrdersFromSeeds();

            UpdateExistingOrdersFromLibris();

            return Result;
        }

        #region Private methods

        private void AddSeedsForNewUserRequestsFromLibris()
        {
            try
            {
                var sigels = ConfigurationManager.AppSettings["librarySigel"].Split(',').Select(x => x.Trim()).ToList();

                foreach (var sigel in sigels)
                {
                    var addressStr = ConfigurationManager.AppSettings["librisApiBaseAddress"] + "/api/userrequests/" + sigel;

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
                            seed.Id = "LIBRIS-LTB-" + req.request_id.Value;

                            if (!OrderWithSeedIdAlreadyExists(seed.Id))
                            {
                                seed.PatronEmail = req.user.email.Value;
                                seed.PatronName = req.user.full_name.Value;
                                seed.PatronCardNumber = req.user.library_card.Value;
                                seed.DeliveryLibrarySigel = sigel;
                                seed.Message = "Författare: " + ReplaceWithNotAvailableIfEmptyString(req.author.Value) + "\n" +
                                    "Titel: " + ReplaceWithNotAvailableIfEmptyString(req.title.Value) + "\n" +
                                    "Utgivning: " + ReplaceWithNotAvailableIfEmptyString(req.imprint.Value) + "\n" +
                                    "ISBN/ISSN: " + ReplaceWithNotAvailableIfEmptyString(req.isxn.Value);
                                seed.MessagePrefix = "LIBRIS LÅNTAGARBESTÄLLNING" + "\n\n" +
                                    ConfigurationManager.AppSettings["librisApiBaseAddress"] + ConfigurationManager.AppSettings["librisApiUserRequestSuffix"] + "\n\n";
                                _seeds.Add(seed);
                            }
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
                foreach (var seed in _seeds)
                {
                    seed.SierraPatronInfo = _patronDataProvider.GetPatronInfoFromLibraryCardNumberOrPersonnummer(seed.PatronCardNumber, seed.PatronCardNumber);
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

                        var eventId = _orderItemManager.GenerateEventId(CREATE_ORDER_FROM_LIBRIS_DATA_EVENT_TYPE);
                        _orderItemManager.AddSierraDataToLog(orderItemNodeId, seed.SierraPatronInfo, eventId);

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

        private void UpdateExistingOrdersFromLibris()
        {
            try
            {
                var orders = GetSearchResultsForAllActiveOrdersThatAreOrderedFromLibris();

                foreach (var order in orders)
                {
                    var addressStr = ConfigurationManager.AppSettings["librisApiBaseAddress"] + "/api/illrequests/Z/" + order.Fields["ProviderOrderId"].ToString();

                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("api-key", ConfigurationManager.AppSettings["librisApiKey"]);
                    var task = httpClient.GetStringAsync(new Uri(addressStr));

                    task.Wait();

                    var illRequestsQueryResult = JsonConvert.DeserializeObject<dynamic>(task.Result);

                    var illRequests = illRequestsQueryResult.ill_requests;
                    var illRequestsCount = illRequestsQueryResult.count.Value;
                    if (illRequestsCount == 0)
                    {
                        _result.Errors++;
                        _result.Messages.Add("Couldn't find any Libris ILL request with the ID: " + order.Fields["ProviderOrderId"].ToString());
                    }
                    else
                    {
                        for (int index = 0; index < illRequestsCount; index++)
                        {
                            var req = illRequests[index];

                            if (order.Fields["Status"] == "03:Beställd" && req.status_code.Value == "6") // Status code 6 is "Negativt svar" in Libris
                            {
                                var eventId = _orderItemManager.GenerateEventId(UPDATE_ORDER_FROM_LIBRIS_DATA_EVENT_TYPE);
                                _orderItemManager.SetStatus(order.Id, "02:Åtgärda", eventId, false, false);
                                _orderItemManager.AddLogItem(order.Id, "LIBRIS", "Negativt svar. " + ConfigurationManager.AppSettings["librisApiBaseAddress"] + "/lf.php?action=notfullfilled&id=" + req.request_id.Value, eventId);
                            }
                            else if (order.Fields["Status"] == "03:Beställd" && req.status_code.Value == "7") // Status code 7 is "Kan reserveras" in Libris
                            {
                                var eventId = _orderItemManager.GenerateEventId(UPDATE_ORDER_FROM_LIBRIS_DATA_EVENT_TYPE);
                                _orderItemManager.SetStatus(order.Id, "02:Åtgärda", eventId, false, false);
                                _orderItemManager.AddLogItem(order.Id, "LIBRIS", "Kan reserveras." + ConfigurationManager.AppSettings["librisApiBaseAddress"] + "/lf.php?action=may_reserve&id=" + req.request_id.Value, eventId);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new SourcePollingException("Error when updating existing orders from Libris.", e);
            }
        }

        private bool OrderWithSeedIdAlreadyExists(string seedId)
        {
            // TODO: Inject Examine or add this to OrderItemManager, which probably should be called something else.
            var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

            var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);

            var query = searchCriteria.RawQuery(@"seedId:" + seedId);

            return searcher.Search(query).Count() > 0;
        }

        private ISearchResults GetSearchResultsForAllActiveOrdersThatAreOrderedFromLibris()
        {
            // TODO: Inject Examine or add this to OrderItemManager, which probably should be called something else.
            var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

            var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);

            var query = searchCriteria.RawQuery(@"(Status:01\:Ny OR 
                 Status:02\:Åtgärda OR
                 Status:03\:Beställd OR
                 Status:04\:Väntar OR
                 Status:09\:Mottagen) AND
                 ProviderName:libris");

            return searcher.Search(query);
        }

        private string ReplaceWithNotAvailableIfEmptyString(string val)
        {
            return (val == "" ? "N/A" : val);
        }

        #endregion
    }
}