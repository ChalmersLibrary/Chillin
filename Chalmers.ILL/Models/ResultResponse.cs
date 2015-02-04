using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    /// <summary>
    /// General Response Object to use in CRUD communication
    /// </summary>
    public class ResultResponse
    {
        [Required]
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}