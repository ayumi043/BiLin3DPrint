using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Bilin3d.Models
{
    public class CarModel {
        [Required(ErrorMessage = "打印材料不能为空")]
        public string MaterialId { get; set; }

        [Required(ErrorMessage = "打印数量不能为空")]
        [Range(1, 9999, ErrorMessage = "打印数量必须在1~9999之间")]
        public int Num { get; set; }

        [Required(ErrorMessage = "尺寸不能为空")]
        public string Size { get; set; }

        [Required(ErrorMessage = "表面积不能为空")]
        public string Area { get; set; }

        [Required(ErrorMessage = "体积不能为空")]
        public float Volume { get; set; }
        public string Weight { get; set; }

        //public decimal Price { get; set; }

        [Required(ErrorMessage = "文件名不能为空")]
        public string FileName { get; set; }


        [Required(ErrorMessage = "供应商id不能为空")]
        public string SupplierId { get; set; }

        //
        public decimal Amount { get; set; }
        public decimal AmountDetail { get; set; }

        public decimal Price { get; set; }
        public decimal Price1 { get; set; }
        public string MatName { get; set; }
        public string MatColor { get; set; }
        public string MatDensity { get; set; }
        public string Accuracy { get; set; }
        public string Delivery { get; set; }
        public int CarDetailId { get; set; }
        public string CarId { get; set; }

        public string SupplierPrinterMaterialId { get; set; }

    }
}