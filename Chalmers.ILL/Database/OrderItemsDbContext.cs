using Chalmers.ILL.Models;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using Umbraco.Core.Logging;
using System.Linq;
using Chalmers.ILL.OrderItems;
using System.Collections.Generic;
using System.Data.Entity.Validation;

namespace Chalmers.ILL.Database
{
    public class OrderItemsDbContext : DbContext
    {
        private static readonly string _connectionStringName = "chillinOrderItemsDb";

        private IOrderItemSearcher _orderItemSearcher;

        public DbSet<OrderItemModel> OrderItems { get; set; }

        public OrderItemsDbContext() : base(_connectionStringName) { }

        public OrderItemsDbContext(IOrderItemSearcher orderItemSearcher) : base(_connectionStringName)
        {
            _orderItemSearcher = orderItemSearcher;
        }

        public override int SaveChanges()
        {
            var orderItemChanges = from e in ChangeTracker.Entries<OrderItemModel>()
                                        where e.State != EntityState.Unchanged
                                        select e;

            var addedItems = new List<OrderItemModel>();
            var modifiedItems = new List<OrderItemModel>();
            var removedItems = new List<OrderItemModel>();

            foreach (var change in orderItemChanges)
            {
                if (change.State == EntityState.Added)
                {
                    change.Entity.CreateDate = DateTime.Now;
                    change.Entity.UpdateDate = DateTime.Now;
                    addedItems.Add(change.Entity);
                }
                else if (change.State == EntityState.Deleted)
                {
                    removedItems.Add(change.Entity);
                }
                else if (change.State == EntityState.Modified)
                {
                    change.Entity.UpdateDate = DateTime.Now;
                    modifiedItems.Add(change.Entity);
                }
            }

            int res = 0;
            try
            {
                res = base.SaveChanges();

                foreach (var item in addedItems)
                {
                    _orderItemSearcher.Added(item);
                }

                foreach (var item in modifiedItems)
                {
                    _orderItemSearcher.Modified(item);
                }

                foreach (var item in removedItems)
                {
                    _orderItemSearcher.Deleted(item);
                }
            }
            catch (DbEntityValidationException e)
            {
                LogHelper.Error<OrderItemsDbContext>("An entity validation exception occured during saving.", e);
                throw e;
            }
            catch (Exception e)
            {
                LogHelper.Error<OrderItemsDbContext>("An error occured during a save to database.", e);
                throw e;
            }

            return res;
        }
    }
}