using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class OrderAttachment
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid DbId { get; set; }

        [Required]
        public string MediaItemNodeId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }
}