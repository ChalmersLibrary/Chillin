namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovedRequiredOnOrderId : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.OrderItemModels", "OrderId", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.OrderItemModels", "OrderId", c => c.String(nullable: false));
        }
    }
}
