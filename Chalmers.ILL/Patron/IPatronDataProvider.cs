using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chalmers.ILL.Models;

namespace Chalmers.ILL.Patron
{
    public interface IPatronDataProvider
    {
        IPatronDataProvider Connect();
        IPatronDataProvider Disconnect();

        SierraModel GetPatronInfoFromLibraryCardNumber(string barcode);
        SierraModel GetPatronInfoFromLibraryCardNumberOrPersonnummer(string barcode, string pnr);
    }
}
