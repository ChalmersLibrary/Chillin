namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIsAnonymizedAutomatically : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "IsAnonymizedAutomatically", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderItemModels", "IsAnonymizedAutomatically");
        }
    }
}
