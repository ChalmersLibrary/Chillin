using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioService
    {
        void InitFolio(string title, string orderId, string barcode, string pickUpServicePoint, bool readOnlyAtLibrary, string patronCardNumber);
        void SetItemToWithdrawn(string id);
    }
}