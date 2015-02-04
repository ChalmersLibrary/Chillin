using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class MailAttachment
    {
        public MailAttachment()
        {
            // NOP
        }

        public MailAttachment(string newTitle, System.IO.MemoryStream newData)
        {
            Title = newTitle;
            Data = newData;
        }

        public string Title { get; set; }
        public System.IO.MemoryStream Data { get; set; }
    }
}