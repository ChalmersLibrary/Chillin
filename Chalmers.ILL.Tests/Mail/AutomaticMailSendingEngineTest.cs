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
        private IOrderItemSearcher GetFakeSearcher(IEnumerable<OrderItemModel> fakeSearchResults)
        {
            return new Chalmers.ILL.OrderItems.Fakes.StubIOrderItemSearcher()
            {
                SearchString = (query) => { return fakeSearchResults; }
            };
        }

        private ISearchCriteria GetFakeSearchCriteria()
        {
            return new Examine.SearchCriteria.Fakes.StubISearchCriteria()
            {
                RawQueryString = (query) => { return GetFakeSearchCriteria(); }
            };
        }

        private IAutomaticMailSendingEngine SetupAutomaticMailSendingEngine(string status, DateTime dueDate, DateTime deliveryDate, AutomaticMailSendTestResult result)
        {
            var fakeSearchResults = new List<OrderItemModel>()
            {
                new OrderItemModel()
                {
                    Status = status,
                    OrderId = "cth-123",
                    PatronName = "John Doe",
                    PatronEmail = "john@doe.com",
                    DueDate = dueDate,
                    DeliveryDate = deliveryDate
                }
            };

            IOrderItemSearcher orderItemsSearcher = GetFakeSearcher(fakeSearchResults);

            ITemplateService templateService = new Templates.Fakes.StubITemplateService()
            {
                GetTemplateDataStringOrderItemModel = (nodeName, orderItem) =>
                {
                    result.MailTemplate = nodeName;
                    return "FEJKMAIL";
                }
            };

            IOrderItemManager orderItemManager = new Chalmers.ILL.OrderItems.Fakes.StubIOrderItemManager()
            {
                SetStatusInt32StringStringBooleanBoolean = (nodeId, statusPrevalue, eventId, doReindex, doSignal) =>
                {
                    result.NewStatus = statusPrevalue;

                    if (doReindex)
                    {
                        result.NumberOfReindexes++;
                    }

                    if (doSignal)
                    {
                        result.NumberOfSignals++;
                    }
                },
                AddLogItemInt32StringStringStringBooleanBoolean = (nodeId, type, msg, eventId, doReindex, doSignal) =>
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
            public string NewStatus { get; set; }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_OnLoanDueDateIsInFiveDays_CourtesyNoticeIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("11:Utlånad", DateTime.Now.AddDays(5), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("CourtesyNoticeMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_OnLoanDueDateWasYesterday_LoanPeriodOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("11:Utlånad", DateTime.Now.AddDays(-1), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_OnLoanDueDateWasFiveDaysAgo_LoanPeriodReallyOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("11:Utlånad", DateTime.Now.AddDays(-5), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodReallyOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_OnLoanDueDateWasTenDaysAgo_LoanPeriodReallyReallyOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("11:Utlånad", DateTime.Now.AddDays(-10), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodReallyReallyOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_OnLoanDueDateIsToday_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("11:Utlånad", DateTime.Now, new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_OnLoanDueDateIsFarIntoTheFuture_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("11:Utlånad", DateTime.Now.AddDays(24), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_OnLoanDueDateHasPassedLongAgo_StatusDoSomething()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("11:Utlånad", DateTime.Now.AddDays(-24), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(1, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual("02:Åtgärda", result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_ClaimedDueDateIsInFiveDays_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("12:Krävd", DateTime.Now.AddDays(5), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_ClaimedDueDateWasYesterday_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("12:Krävd", DateTime.Now.AddDays(-1), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_ClaimedDueDateWasFiveDaysAgo_LoanPeriodReallyOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("12:Krävd", DateTime.Now.AddDays(-5), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodReallyOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_ClaimedDueDateWasTenDaysAgo_LoanPeriodReallyReallyOverMailIsSentOut()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("12:Krävd", DateTime.Now.AddDays(-10), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(2, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("LoanPeriodReallyReallyOverMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_ClaimedDueDateIsToday_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("12:Krävd", DateTime.Now, new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_ClaimedDueDateIsFarIntoTheFuture_NothingHappens()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("12:Krävd", DateTime.Now.AddDays(24), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_ClaimedDueDateHasPassedLongAgo_StatusDoSomething()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("12:Krävd", DateTime.Now.AddDays(-24), new DateTime(1970, 1, 1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(1, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual("02:Åtgärda", result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_InTransitDeliveryDateMoreThanTwoDaysAgo_StatusDelivered()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("13:Transport", new DateTime(1970, 1, 1), DateTime.Now.AddDays(-3), result).SendOutMailsThatAreDue();

                Assert.AreEqual(3, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(1, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(1, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual("ArticleAvailableInInfodiskMailTemplate", result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual("05:Levererad", result.NewStatus, "The new status was not as expected.");
            }
        }

        [TestMethod]
        public void SendOutMailsThatAreDue_InTransitDeliveryDateLessThanTwoDaysAgo_StatusDelivered()
        {
            using (ShimsContext.Create())
            {
                var result = new AutomaticMailSendTestResult();

                SetupAutomaticMailSendingEngine("13:Transport", new DateTime(1970, 1, 1), DateTime.Now.AddDays(-1), result).SendOutMailsThatAreDue();

                Assert.AreEqual(0, result.NumberOfLogMessages, "Number of messages logged was not as expected.");
                Assert.AreEqual(0, result.NumberOfReindexes, "Number of reindexes was not as expected.");
                Assert.AreEqual(0, result.NumberOfSignals, "Number of signals was not as expected.");
                Assert.AreEqual(null, result.MailTemplate, "The fetched template was not as expected.");
                Assert.AreEqual(null, result.NewStatus, "The new status was not as expected.");
            }
        }
    }
}
