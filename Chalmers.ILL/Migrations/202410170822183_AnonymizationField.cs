namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AnonymizationField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "IsAnonymized", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderItemModels", "IsAnonymized");
        }
    }
}
