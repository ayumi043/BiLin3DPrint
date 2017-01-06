using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bilin3d.Models {

    [Alias("t_user")]
    public class UserModel {
        public int Id { get; set; }                         
        public Guid UserGuid { get; set; }
        public string Email { get; set; }
        public string NickName { get; set; }
        public string Tel { get; set; }
        public string Avatars { get; set; }
        public string PassWord { get; set; }
        public int PointTotal { get; set; }
        public int PointRemain { get; set; }
        public decimal Expense { get; set; }
        public string Balance { get; set; }
        public int State { get; set; }   // 0.正常、1.停用、2.删除
        public string SupplierId { get; set; }
        public string EditTime { get; set; }   
        public string CreateTime { get; set; }
    }
}