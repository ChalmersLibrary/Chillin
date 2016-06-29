using Chalmers.ILL.Models;
using System.Data.Entity;

namespace Chalmers.ILL.Database
{
    public class OrderItemsDbContext : DbContext
    {
        private static readonly string _connectionStringName = "chillinOrderItemsDb";

        public DbSet<OrderItemModel> OrderItems { get; set; }

        public OrderItemsDbContext() : base(_connectionStringName) { }
    }
}