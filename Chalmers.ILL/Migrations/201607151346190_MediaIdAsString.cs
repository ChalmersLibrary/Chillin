namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MediaIdAsString : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.OrderAttachments", "MediaItemNodeId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.OrderAttachments", "MediaItemNodeId", c => c.Int(nullable: false));
        }
    }
}
