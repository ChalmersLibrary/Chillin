using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Mail
{
    public interface IAutomaticMailSendingEngine
    {
        IEnumerable<MailOperationResult> SendOutMailsThatAreDue();
        void RemoveOldSentMails();
    }
}
