using Chalmers.ILL.Models;

namespace Chalmers.ILL.Services
{
    public interface IFolioCirculationService
    {
        Circulation Post(CirculationBasic item);
    }
}