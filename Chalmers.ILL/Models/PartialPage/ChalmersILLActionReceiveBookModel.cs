namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionReceiveBookModel : OrderItemPageModelBase
    {
        public string BookAvailableMailTemplate { get; set; }
        public string TitleInformation { get; set; }

        public ChalmersILLActionReceiveBookModel(OrderItemModel orderItemModel, string standardTitleText) : base(orderItemModel) 
        {
            TitleInformation = SetTitleInformation(orderItemModel.TitleInformation, orderItemModel.Reference, standardTitleText);
        }

        private string SetTitleInformation(string titleInformation, string reference, string text)
        {
            if (string.IsNullOrEmpty(titleInformation))
            {
                return $"{reference} {text}";
            }
            if (titleInformation.Contains(text))
            {
                return titleInformation;
            }
            return $"{titleInformation} {text}";
        }
    }
}