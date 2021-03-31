namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionReceiveBookModel : OrderItemPageModelBase
    {
        public string BookAvailableMailTemplate { get; set; }
        public string StandardTitleText { get; set; }

        public ChalmersILLActionReceiveBookModel(OrderItemModel orderItemModel, string standardTitleText) : base(orderItemModel) 
        {
            StandardTitleText = standardTitleText;
        }
    }
}