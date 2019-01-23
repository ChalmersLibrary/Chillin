namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPurchaseLibrary : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "PurchaseLibrary", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderItemModels", "PurchaseLibrary");
        }
    }
}
