using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioHoldingService
    {
        Holding Post(HoldingBasic item);
    }
}