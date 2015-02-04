using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Chalmers.ILL.Models
{
    public class PasswordModel
    {
        [Required]
        [Display(Name = "Nuvarande lösenord")]
        public string CurrentPassword { get; set; }

        [Required]
        [Display(Name = "Nytt lösenord")]
        public string NewPassword { get; set; }
    }
}
