namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCid : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SierraModels", "cid", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SierraModels", "cid");
        }
    }
}
