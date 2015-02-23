using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chalmers.ILL.Controllers.SurfaceControllers;
using Examine;
using Examine.SearchCriteria;
using Chalmers.ILL.Statistics;
using System.Collections.Generic;
using Microsoft.QualityTools.Testing.Fakes;
using System.Collections;

namespace Chalmers.ILL.Tests.Statistics
{
    [TestClass]
    public class DefaultStatCalcTests
    {
        private ISearcher GetFakeSearcher()
        {
            return new Examine.Fakes.StubISearcher()
            {
                CreateSearchCriteria = () => { return GetFakeSearchCriteria(); },
                CreateSearchCriteriaBooleanOperation = (defaultOperation) => { return GetFakeSearchCriteria(); },
                CreateSearchCriteriaString = (type) => { return GetFakeSearchCriteria(); },
                CreateSearchCriteriaStringBooleanOperation = (type, defaultOperation) => { return GetFakeSearchCriteria(); },
                SearchISearchCriteria = (searchParameters) => { return GetFakeSearchResults(); },
                SearchStringBoolean = (searchText, useWildcards) => { return GetFakeSearchResults(); }
            };
        }

        private ISearchCriteria GetFakeSearchCriteria()
        {
            return new Examine.SearchCriteria.Fakes.StubISearchCriteria()
            {
                RawQueryString = (query) => { return GetFakeSearchCriteria(); }
            };
        }

        private ISearchResults GetFakeSearchResults()
        {
            return new Examine.Fakes.StubISearchResults()
            {
                GetEnumerator = () => {
                    var tempList = new List<SearchResult>();
                    tempList.Add(GetFakeSearchResult());
                    tempList.Add(GetFakeSearchResult());
                    tempList.Add(GetFakeSearchResult());
                    tempList.Add(GetFakeSearchResult());
                    tempList.Add(GetFakeSearchResult());
                    tempList.Add(GetFakeSearchResult());
                    return tempList.GetEnumerator(); 
                }
            };
        }
        private SearchResult GetFakeSearchResult()
        {
            return new Examine.Fakes.ShimSearchResult()
                    {
                        FieldsGet = () =>
                        {
                            var val = new Dictionary<string, string>();
                            val.Add("Log", "[{\"OrderItemNodeId\":0,\"NodeId\":0,\"Type\":\"STATUS\",\"Message\":\"Status ändrad från Mottagen till Levererad\",\"MemberName\":\"Lars Andersson\",\"CreateDate\":\"2014-03-04T08:39:07.0230965+01:00\"}]");
                            val.Add("createDate", "20140303080400000");
                            return val;
                        }
                    };
        }

        [TestMethod]
        public void CalculateDataPointValue_UnknownCommand_ExceptionThrown()
        {
            var statCalc = new DefaultStatCalc();

            try
            {
                statCalc.CalculateDataPointValue(GetFakeSearchResults(), "TJOLAHOPP");
                Assert.Fail("Should have thrown an exception.");
            }
            catch (Exception e)
            {
                Assert.IsTrue(String.Equals("Unknown calculation type.", e.Message));
            }
        }

        [TestMethod]
        public void CalculateDataPointValue_CountCommand_ReturnsCorrectValue()
        {
            using (ShimsContext.Create())
            {
                var statCalc = new DefaultStatCalc();

                var val = statCalc.CalculateDataPointValue(GetFakeSearchResults(), "COUNT");

                Assert.AreEqual(6, val);
            }
        }

        [TestMethod]
        public void CalculateDataPointValue_AverageOrderLengthCommand_ReturnsCorrectValue()
        {
            using (ShimsContext.Create())
            {
                var statCalc = new DefaultStatCalc();

                var val = statCalc.CalculateDataPointValue(GetFakeSearchResults(), "AVERAGE_ORDER_LENGTH");

                Assert.AreEqual(1475, val);
            }
        }

        [TestMethod]
        public void CalculateDataPointValue_MedianOrderLengthCommand_ReturnsCorrectValue()
        {
            using (ShimsContext.Create())
            {
                var statCalc = new DefaultStatCalc();

                var val = statCalc.CalculateDataPointValue(GetFakeSearchResults(), "MEDIAN_ORDER_LENGTH");

                Assert.AreEqual(1475, val);
            }
        }
    }
}
