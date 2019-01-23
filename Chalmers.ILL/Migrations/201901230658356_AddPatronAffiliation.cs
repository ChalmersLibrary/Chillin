namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPatronAffiliation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "PatronAffiliation", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderItemModels", "PatronAffiliation");
        }
    }
}
