namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddReadOnlyAtLibrary : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "ReadOnlyAtLibrary", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderItemModels", "ReadOnlyAtLibrary");
        }
    }
}
