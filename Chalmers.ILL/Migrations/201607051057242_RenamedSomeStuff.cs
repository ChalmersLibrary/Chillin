namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenamedSomeStuff : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderItemModels", "TypeId", c => c.Int(nullable: false));
            AddColumn("dbo.OrderItemModels", "StatusId", c => c.Int(nullable: false));
            AddColumn("dbo.OrderItemModels", "PreviousStatusId", c => c.Int(nullable: false));
            AddColumn("dbo.OrderItemModels", "LastDeliveryStatusId", c => c.Int(nullable: false));
            AddColumn("dbo.OrderItemModels", "DeliveryLibraryId", c => c.Int(nullable: false));
            AddColumn("dbo.OrderItemModels", "CancellationReasonId", c => c.Int(nullable: false));
            AddColumn("dbo.OrderItemModels", "PurchasedMaterialId", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderItemModels", "Type", c => c.String());
            AlterColumn("dbo.OrderItemModels", "Status", c => c.String());
            AlterColumn("dbo.OrderItemModels", "PreviousStatus", c => c.String());
            AlterColumn("dbo.OrderItemModels", "LastDeliveryStatus", c => c.String());
            AlterColumn("dbo.OrderItemModels", "DeliveryLibrary", c => c.String());
            AlterColumn("dbo.OrderItemModels", "CancellationReason", c => c.String());
            AlterColumn("dbo.OrderItemModels", "PurchasedMaterial", c => c.String());
            DropColumn("dbo.OrderItemModels", "TypePrevalue");
            DropColumn("dbo.OrderItemModels", "StatusPrevalue");
            DropColumn("dbo.OrderItemModels", "PreviousStatusPrevalue");
            DropColumn("dbo.OrderItemModels", "LastDeliveryStatusPrevalue");
            DropColumn("dbo.OrderItemModels", "DeliveryLibraryPrevalue");
            DropColumn("dbo.OrderItemModels", "CancellationReasonPrevalue");
            DropColumn("dbo.OrderItemModels", "PurchasedMaterialPrevalue");
        }
        
        public override void Down()
        {
            AddColumn("dbo.OrderItemModels", "PurchasedMaterialPrevalue", c => c.String());
            AddColumn("dbo.OrderItemModels", "CancellationReasonPrevalue", c => c.String());
            AddColumn("dbo.OrderItemModels", "DeliveryLibraryPrevalue", c => c.String());
            AddColumn("dbo.OrderItemModels", "LastDeliveryStatusPrevalue", c => c.String());
            AddColumn("dbo.OrderItemModels", "PreviousStatusPrevalue", c => c.String());
            AddColumn("dbo.OrderItemModels", "StatusPrevalue", c => c.String());
            AddColumn("dbo.OrderItemModels", "TypePrevalue", c => c.String());
            AlterColumn("dbo.OrderItemModels", "PurchasedMaterial", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderItemModels", "CancellationReason", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderItemModels", "DeliveryLibrary", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderItemModels", "LastDeliveryStatus", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderItemModels", "PreviousStatus", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderItemModels", "Status", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderItemModels", "Type", c => c.Int(nullable: false));
            DropColumn("dbo.OrderItemModels", "PurchasedMaterialId");
            DropColumn("dbo.OrderItemModels", "CancellationReasonId");
            DropColumn("dbo.OrderItemModels", "DeliveryLibraryId");
            DropColumn("dbo.OrderItemModels", "LastDeliveryStatusId");
            DropColumn("dbo.OrderItemModels", "PreviousStatusId");
            DropColumn("dbo.OrderItemModels", "StatusId");
            DropColumn("dbo.OrderItemModels", "TypeId");
        }
    }
}
