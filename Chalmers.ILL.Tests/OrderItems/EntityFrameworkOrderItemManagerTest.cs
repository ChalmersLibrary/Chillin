using System.Collections.Generic;
using System.Reflection;
using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.UmbracoApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chalmers.ILL.Tests.OrderItems
{
    [TestClass]
    public class EntityFrameworkOrderItemManagerTest
    {
        // Regression test for a bug where newly created, still-unclassified order items
        // (TypeId == -1, i.e. no Bok/Artikel/Inköpsförslag chosen yet) ended up with Type
        // as a real C# null instead of "", crashing the order list view's
        // type.ToString() call. Introduced when "Goodbye umbraco data types." rewrote the
        // old ternary (which always assigned a string) into an if with no else branch.
        [TestMethod]
        public void FillOutStuff_UnclassifiedType_DefaultsToEmptyStringNotNull()
        {
            var manager = new EntityFrameworkOrderItemManager(new StubChillinOrderConfiguration(), new StubOrderItemSearcher());
            var orderItem = new OrderItemModel(); // TypeId == -1 by default, i.e. unclassified

            InvokeFillOutStuff(manager, orderItem);

            Assert.AreEqual("", orderItem.Type);
            Assert.AreEqual("", orderItem.DeliveryLibrary);
            Assert.AreEqual("", orderItem.CancellationReason);
            Assert.AreEqual("", orderItem.PurchasedMaterial);
        }

        [TestMethod]
        public void FillOutStuff_ClassifiedType_LooksUpValueFromConfig()
        {
            var manager = new EntityFrameworkOrderItemManager(new StubChillinOrderConfiguration(), new StubOrderItemSearcher());
            var orderItem = new OrderItemModel { TypeId = 42 };

            InvokeFillOutStuff(manager, orderItem);

            Assert.AreEqual("Inköpsförslag", orderItem.Type);
        }

        private static void InvokeFillOutStuff(EntityFrameworkOrderItemManager manager, OrderItemModel orderItem)
        {
            var method = typeof(EntityFrameworkOrderItemManager).GetMethod("FillOutStuff", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(manager, new object[] { orderItem });
        }

        class StubChillinOrderConfiguration : IChillinOrderConfiguration
        {
            public List<DropdownOption> GetAvailableTypes() => new List<DropdownOption>();
            public List<DropdownOption> GetAvailableStatuses() => new List<DropdownOption>();
            public List<DropdownOption> GetAvailableDeliveryLibraries() => new List<DropdownOption>();
            public List<DropdownOption> GetAvailableCancellationReasons() => new List<DropdownOption>();
            public List<DropdownOption> GetAvailablePurchasedMaterials() => new List<DropdownOption>();
            public string GetValueById(int id) => id == 42 ? "Inköpsförslag" : null;
            public int GetIdByValue(string listKey, string value) => -1;
            public void PopulateModelWithAvailableValues(OrderItemPageModelBase model) { }
        }

        class StubOrderItemSearcher : IOrderItemSearcher
        {
            public IEnumerable<OrderItemModel> Search(string query) => new List<OrderItemModel>();
            public SearchResult Search(string query, int start, int size) => new SearchResult { Items = new List<OrderItemModel>(), Count = 0 };
            public IEnumerable<OrderItemModel> Search(string query, int size, string[] fields) => new List<OrderItemModel>();
            public IEnumerable<string> AggregatedProviders() => new List<string>();
            public void Added(OrderItemModel item) { }
            public void Modified(OrderItemModel item) { }
            public void Deleted(OrderItemModel item) { }
        }
    }
}
