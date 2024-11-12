using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Mail
{
    public interface IMailService
    {
        void SendMail(OutgoingMailModel mailModel);
        void DeleteOldMessagesFromFolder(string folder, DateTime oldLimit);
    }
}
