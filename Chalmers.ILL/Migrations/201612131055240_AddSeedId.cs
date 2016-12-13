namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSeedId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "SeedId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderItemModels", "SeedId");
        }
    }
}
