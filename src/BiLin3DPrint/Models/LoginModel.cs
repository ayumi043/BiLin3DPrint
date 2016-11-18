using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Bilin3d.Models
{
    public class LoginModel
    {
        //[Required]
        //public string UserName { get; set; }

        [Required(ErrorMessage = "电子邮箱不能为空")]
        public string Email { get; set; }

         [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}