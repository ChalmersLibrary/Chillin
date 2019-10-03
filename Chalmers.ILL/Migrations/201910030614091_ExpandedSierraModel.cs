namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ExpandedSierraModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SierraModels", "pnum", c => c.String());
            AddColumn("dbo.SierraModels", "expdate", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SierraModels", "expdate");
            DropColumn("dbo.SierraModels", "pnum");
        }
    }
}
