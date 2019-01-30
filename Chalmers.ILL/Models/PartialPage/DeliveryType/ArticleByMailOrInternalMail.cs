namespace Chalmers.ILL.Models.PartialPage.DeliveryType
{
    public class ArticleByMailOrInternalMail : OrderItemPageModelBase
    {
        public bool DrmWarning { get; set; }
        public string ArticleDeliveryByInternpostTemplate { get; set; }
        public string ArticleDeliveryByPostTemplate { get; set; }

        public ArticleByMailOrInternalMail(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}