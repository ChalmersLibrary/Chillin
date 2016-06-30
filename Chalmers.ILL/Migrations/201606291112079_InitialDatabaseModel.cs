namespace Chalmers.ILL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialDatabaseModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OrderItemModels",
                c => new
                    {
                        NodeId = c.Int(nullable: false, identity: true),
                        OrderId = c.String(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                        UpdateDate = c.DateTime(nullable: false),
                        FollowUpDate = c.DateTime(nullable: false),
                        FollowUpDateIsDue = c.Boolean(nullable: false),
                        ContentVersionsCount = c.Int(nullable: false),
                        OriginalOrder = c.String(),
                        Reference = c.String(),
                        Type = c.Int(nullable: false),
                        TypePrevalue = c.String(),
                        Status = c.Int(nullable: false),
                        StatusPrevalue = c.String(),
                        StatusString = c.String(),
                        PreviousStatus = c.Int(nullable: false),
                        PreviousStatusPrevalue = c.String(),
                        PreviousStatusString = c.String(),
                        LastDeliveryStatus = c.Int(nullable: false),
                        LastDeliveryStatusPrevalue = c.String(),
                        LastDeliveryStatusString = c.String(),
                        DeliveryLibrary = c.Int(nullable: false),
                        DeliveryLibraryPrevalue = c.String(),
                        CancellationReason = c.Int(nullable: false),
                        CancellationReasonPrevalue = c.String(),
                        PurchasedMaterial = c.Int(nullable: false),
                        PurchasedMaterialPrevalue = c.String(),
                        PatronName = c.String(),
                        PatronEmail = c.String(),
                        PatronCardNo = c.String(),
                        ProviderName = c.String(),
                        ProviderOrderId = c.String(),
                        ProviderInformation = c.String(),
                        ProviderDueDate = c.DateTime(nullable: false),
                        EditedBy = c.String(),
                        EditedByMemberName = c.String(),
                        EditedByCurrentMember = c.Boolean(nullable: false),
                        Log = c.String(),
                        Attachments = c.String(),
                        SierraInfoStr = c.String(),
                        DrmWarning = c.String(),
                        DeliveryLibrarySameAsHomeLibrary = c.Boolean(nullable: false),
                        DueDate = c.DateTime(nullable: false),
                        DeliveryDate = c.DateTime(nullable: false),
                        BookId = c.String(),
                        SierraInfo_DbId = c.Guid(),
                    })
                .PrimaryKey(t => t.NodeId)
                .ForeignKey("dbo.SierraModels", t => t.SierraInfo_DbId)
                .Index(t => t.SierraInfo_DbId);
            
            CreateTable(
                "dbo.OrderAttachments",
                c => new
                    {
                        DbId = c.Guid(nullable: false, identity: true),
                        MediaItemNodeId = c.Int(nullable: false),
                        Title = c.String(),
                        Link = c.String(),
                        OrderItemModel_NodeId = c.Int(),
                    })
                .PrimaryKey(t => t.DbId)
                .ForeignKey("dbo.OrderItemModels", t => t.OrderItemModel_NodeId)
                .Index(t => t.OrderItemModel_NodeId);
            
            CreateTable(
                "dbo.LogItems",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        OrderItemNodeId = c.Int(nullable: false),
                        NodeId = c.Int(nullable: false),
                        EventId = c.String(),
                        Type = c.String(),
                        Message = c.String(),
                        MemberName = c.String(),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.OrderItemModels", t => t.NodeId, cascadeDelete: true)
                .Index(t => t.NodeId);
            
            CreateTable(
                "dbo.SierraModels",
                c => new
                    {
                        DbId = c.Guid(nullable: false, identity: true),
                        id = c.String(),
                        barcode = c.String(),
                        ptype = c.Int(nullable: false),
                        email = c.String(),
                        first_name = c.String(),
                        last_name = c.String(),
                        mblock = c.String(),
                        home_library = c.String(),
                        home_library_pretty_name = c.String(),
                        record_id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.DbId);
            
            CreateTable(
                "dbo.SierraAddressModels",
                c => new
                    {
                        DbId = c.Guid(nullable: false, identity: true),
                        addresscount = c.String(),
                        addr1 = c.String(),
                        addr2 = c.String(),
                        addr3 = c.String(),
                        village = c.String(),
                        city = c.String(),
                        region = c.String(),
                        postal_code = c.String(),
                        country = c.String(),
                        SierraModel_DbId = c.Guid(),
                    })
                .PrimaryKey(t => t.DbId)
                .ForeignKey("dbo.SierraModels", t => t.SierraModel_DbId)
                .Index(t => t.SierraModel_DbId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderItemModels", "SierraInfo_DbId", "dbo.SierraModels");
            DropForeignKey("dbo.SierraAddressModels", "SierraModel_DbId", "dbo.SierraModels");
            DropForeignKey("dbo.LogItems", "NodeId", "dbo.OrderItemModels");
            DropForeignKey("dbo.OrderAttachments", "OrderItemModel_NodeId", "dbo.OrderItemModels");
            DropIndex("dbo.SierraAddressModels", new[] { "SierraModel_DbId" });
            DropIndex("dbo.LogItems", new[] { "NodeId" });
            DropIndex("dbo.OrderAttachments", new[] { "OrderItemModel_NodeId" });
            DropIndex("dbo.OrderItemModels", new[] { "SierraInfo_DbId" });
            DropTable("dbo.SierraAddressModels");
            DropTable("dbo.SierraModels");
            DropTable("dbo.LogItems");
            DropTable("dbo.OrderAttachments");
            DropTable("dbo.OrderItemModels");
        }
    }
}
