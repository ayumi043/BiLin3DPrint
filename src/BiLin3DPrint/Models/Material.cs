using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bilin3d.Models {

    [Alias("T_Material")]
    public class Material {
        public string MaterialId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Accuracy { get; set; }
        public string Delivery { get; set; }
        public string Density { get; set; }
        public decimal Price { get; set; }
        public decimal Price1 { get; set; }
        public int State { get; set; }   // 0.正常、1.停用、2.删除
    }
}