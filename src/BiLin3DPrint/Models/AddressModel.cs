using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Bilin3d.Models
{
    public class AddressModel {
        [Required(ErrorMessage = "收货人不能为空")]
        public string Consignee { get; set; }

        [Required(ErrorMessage = "省份不能为空")]
        public string Province { get; set; }

        [Required(ErrorMessage = "市不能为空")]
        public string City { get; set; }

        public string Dist { get; set; }

        [Required(ErrorMessage = "详细地址不能为空")]
        public string Address { get; set; }

        [Required(ErrorMessage = "手机号码不能为空")]
        public string Tel { get; set; }

        public int Id { get; set; }
        public string Company { get; set; }
        public string State { get; set; }
    }
}