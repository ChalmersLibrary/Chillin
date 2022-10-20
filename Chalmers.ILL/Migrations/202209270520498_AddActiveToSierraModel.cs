namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddActiveToSierraModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SierraModels", "active", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SierraModels", "active");
        }
    }
}
