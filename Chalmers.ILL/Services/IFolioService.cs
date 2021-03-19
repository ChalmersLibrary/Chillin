using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioService
    {
        IFolioService Connect();
        void InitFolio(InstanceBasic instanceBasic, string barcode, string pickUpServicePoint, bool readOnlyAtLibrary, string patronCardNumber);
    }
}