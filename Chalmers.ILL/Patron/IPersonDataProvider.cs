using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Patron
{
    public interface IPersonDataProvider
    {
        SierraModel GetPatronInfoFromLibraryCidPersonnummerOrEmail(string cidOrPersonnummer, string email);
    }
}
