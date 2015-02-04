using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class OrderAttachment
    {
        [Required]
        public int MediaItemNodeId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }
}