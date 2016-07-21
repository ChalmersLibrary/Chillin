namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Chalmers.ILL.Database.OrderItemsDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(Chalmers.ILL.Database.OrderItemsDbContext context)
        {
            context.Database.ExecuteSqlCommand("DBCC CHECKIDENT('OrderItemModels', RESEED, 100000);");
        }
    }
}
