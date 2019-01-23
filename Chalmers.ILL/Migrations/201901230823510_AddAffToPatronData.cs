namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAffToPatronData : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SierraModels", "aff", c => c.String());
            DropColumn("dbo.SierraModels", "pnum");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SierraModels", "pnum", c => c.String());
            DropColumn("dbo.SierraModels", "aff");
        }
    }
}
