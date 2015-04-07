using Chalmers.ILL.Mail;
using Chalmers.ILL.Templates;
using Examine;
using Examine.SearchCriteria;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.QualityTools.Testing.Fakes;
using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;

namespace Chalmers.ILL.Tests.Mail
{
    [TestClass]
    public class AutomaticMailSendingEngineTest
    {
        private ISearcher GetFakeSearcher(ISearchResults fakeSearchResults)
        {
            return new Examine.Fakes.StubISearcher()
            {
                CreateSearchCriteria = () => { return GetFakeSearchCriteria(); },
                CreateSearchCriteriaBooleanOperation = (defaultOperation) => { return GetFakeSearchCriteria(); },
                CreateSearchCriteriaString = (type) => { return GetFakeSearchCriteria(); },
                CreateSearchCriteriaStringBooleanOperation = (type, defaultOperation) => { return GetFakeSearchCriteria(); },
                SearchISearchCriteria = (searchParameters) => { return fakeSearchResults; },
                SearchStringBoolean = (searchText, useWildcards) => { return fakeSearchResults; }
            };
        }

        private ISearchCriteria GetFakeSearchCriteria()
        {
            return new Examine.SearchCriteria.Fakes.StubISearchCriteria()
            {
                RawQueryString = (query) => { return GetFakeSearchCriteria(); }
            };
        }

        private IAutomaticMailSendingEngine SetupAutomaticMailSendingEngine(DateTime dueDate, AutomaticMailSendTestResult result)
        {
            var fakeSearchResults = new Examine.Fakes.StubISearchResults()
            {
                GetEnumerator = () =>
                {
                    var tempList = new List<SearchResult>();
                    tempList.Add(new Examine.Fakes.ShimSearchResult()
                    {
                        IdGet = () => { return 0; },
                        FieldsGet = () =>
                        {
                            var val = new Dictionary<string, string>();
                            val.Add("OrderId", "cth-123");
                            val.Add("PatronName", "John Doe");
                            val.Add("PatronEmail", "john@doe.com");
                            val.Add("DueDate", dueDate.ToString("yyyyMMddHHmmssfff"));
                            return val;
                        }
                    });
                    return tempList.GetEnumerator();
                }
            };

            ISearcher orderItemsSearcher = GetFakeSearcher(fakeSearchResults);

            ITemplateService templateService = new Templates.Fakes.StubITemplateService()
            {
                GetTemplateDataStringOrderItemModel = (nodeName, orderItem) =>
                {
                    result.MailTemplate = nodeName;
                    return "FEJKMAIL";
                }
            };

            IOrderItemManager orderItemManager = new OrderItems.Fakes.StubIOrderItemManager()
            {
                AddLogItemInt32StringStringBooleanBoolean = (nodeId, type, msg, doReindex, doSignal) =>
                {
                    result.NumberOfLogMessages++;

                    if (doReindex)
                    {
                        result.NumberOfReindexes++;
                    }

                    if (doSignal)
                    {
                        result.NumberOfSignals++;
                    }
                }
            };

            IMailService mailService = new Chalmers.ILL.Mail.Fakes.StubIMailService()
            {
                SendMailOutgoingMailModel = (mailModel) =>
                {
                    Assert.AreEqual("cth-123", mailModel.OrderId, "The order id was not as expected.");
                    Assert.AreEqual("John Doe", mailModel.recipientName, "The recipient name was not as expected.");
                    Assert.AreEqual("john@doe.com", mailModel.recipientEmail, "The recipient email address was not as expected.");
                    Assert.AreEqual("FEJKMAIL", mailModel.message, "The sent email message was not as expected.");
                }
            };

            return new AutomaticMailSendingEngine(orderItemsSearcher, templateService, orderItemManager, mailService);
        }

        private class AutomaticMailSendTestResult
        {
            public int NumberOfLogMessages { get; set; }
            public int NumberOfReindexes { get; set; }
            public int NumberOfSignals { get; set; }
            public string MailTemplate { get; set; }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_DueDateIsInFiveDays_CourtesyNoticeIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine(DateTime.Now.AddDays(5), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("CourtesyNoticeMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_DueDateWasYesterday_LoanPeriodOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine(DateTime.Now.AddDays(-1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_DueDateWasFiveDaysAgo_LoanPeriodReallyOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine(DateTime.Now.AddDays(-5), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodReallyOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_DueDateWasTenDaysAgo_LoanPeriodReallyReallyOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine(DateTime.Now.AddDays(-10), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodReallyReallyOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_DueDateIsToday_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine(DateTime.Now, result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_DueDateIsFarIntoTheFuture_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine(DateTime.Now.AddDays(24), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_DueDateHasPassedLongAgo_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine(DateTime.Now.AddDays(-24), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
            }
        }
    }
}
