using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chalmers.ILL.Models;

namespace Chalmers.ILL.Patron
{
    interface IPatronDataProvider
    {
        void Connect(string connectionString);
        void Disconnect();
        SierraModel GetPatronInfoFromLibraryCardNumberOrPersonnummer(string barcode, string pnr);
    }
}
