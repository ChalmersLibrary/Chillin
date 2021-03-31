using Chalmers.ILL.Models;
using Chalmers.ILL.Models.PartialPage.Settings;

namespace Chalmers.ILL.Repositories
{
    public interface IChillinTextRepository
    {
        void Put(string id, ChillinText chillinText);
        ChillinTextDto All();
        ChillinText ByTextField(string textField);
    }
}