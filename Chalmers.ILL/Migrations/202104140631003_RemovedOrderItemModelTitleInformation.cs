namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovedOrderItemModelTitleInformation : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.OrderItemModels", "TitleInformation");
        }
        
        public override void Down()
        {
            AddColumn("dbo.OrderItemModels", "TitleInformation", c => c.String());
        }
    }
}
