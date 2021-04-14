namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddOrderItemModelTitleInformation1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "TitleInformation", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderItemModels", "TitleInformation");
        }
    }
}
