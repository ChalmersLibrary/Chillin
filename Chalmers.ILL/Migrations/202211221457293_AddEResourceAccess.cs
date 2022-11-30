namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEResourceAccess : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SierraModels", "e_resource_access", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SierraModels", "e_resource_access");
        }
    }
}
