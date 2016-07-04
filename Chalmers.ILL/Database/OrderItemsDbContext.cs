using Chalmers.ILL.Models;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using Umbraco.Core.Logging;
using System.Linq;

namespace Chalmers.ILL.Database
{
    public class OrderItemsDbContext : DbContext
    {
        private static readonly string _connectionStringName = "chillinOrderItemsDb";

        public DbSet<OrderItemModel> OrderItems { get; set; }

        public OrderItemsDbContext() : base(_connectionStringName) { }


        public override int SaveChanges()
        {
            var orderItemChanges = from e in ChangeTracker.Entries<OrderItemModel>()
                                        where e.State != EntityState.Unchanged
                                        select e;

            foreach (var change in orderItemChanges)
            {
                if (change.State == EntityState.Added)
                {

                }
                else if (change.State == EntityState.Deleted)
                {

                }
                else if (change.State == EntityState.Modified)
                {

                }
            }

            int res = 0;
            try
            {
                res = base.SaveChanges();
            }
            catch (Exception e)
            {
                LogHelper.Error<OrderItemsDbContext>("An error occured during a save to database. Here is what we were trying to save: " + JsonConvert.SerializeObject(orderItemChanges), e);
                throw e;
            }

            return res;
        }
    }
}