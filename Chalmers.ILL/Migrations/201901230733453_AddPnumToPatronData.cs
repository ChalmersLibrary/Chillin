namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPnumToPatronData : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SierraModels", "pnum", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SierraModels", "pnum");
        }
    }
}
