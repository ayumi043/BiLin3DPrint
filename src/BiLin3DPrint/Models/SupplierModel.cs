using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Bilin3d.Models.CustomAnnotations;

namespace Bilin3d.Models {
    public class SupplierModel {
        public string SupplierId { get; set; }

        [Required(ErrorMessage = "电话不能为空")]
        public string Tel { get; set; }

        [Required(ErrorMessage = "QQ不能为空")]
        public string QQ { get; set; }

        [Required(ErrorMessage = "地址不能为空")]
        public string Address { get; set; }

        [Required(ErrorMessage = "物流政策不能为空")]
        public string ExpressId { get; set; }

        public string Logo { get; set; }

        //[RequiredIf("Ftype", "0",ErrorMessage = "身份证不能为空")]
        //[RegularExpression(@"(^\d{18}$)|(^\d{15}$)", ErrorMessage = "请输入正确的身份证号码")]
        public string IdCard { get; set; }

        [Required(ErrorMessage = "名称不能为空")]
        public string Fname { get; set; }

        public string IdCardPic1 { get; set; }
        public string IdCardPic2 { get; set; }
        public string CompanyName { get; set; }
        public string Capital { get; set; }
        public string Fcode { get; set; }
        public string BlicensePic { get; set; }

        [Range(0, 1, ErrorMessage = "用户类型错误")]
        public string Ftype { get; set; }
        public string Lng { get; set; }
        public string Lat { get; set; }
        public string State { get; set; }


        // p1,p2 判断2点的距离，用于在结果中按距离进行排序(客户端排序)
        public Point p1 { get; set; } 
        public Point p2 { get; set; }
    }

    public class Point {
        public string Lng { get; set; }
        public string Lat { get; set; }
    }

    /*
     * http://blog.csdn.net/pleasurelong/article/details/26855049
    def rad(d):
        """to弧度
        """
        return d* math.pi / 180.0
    def distanceByLatLon(lat1, lon1, lat2, lon2):
        """通过经纬度计算距离
        """
        EARTH_RADIUS = 6378.137
        radLat1 = rad(lat1)
        radLat2 = rad(lat2)
        a = radLat1 - radLat2
        b = rad(lon1) - rad(lon2)
        s = 2 * math.asin(math.sqrt(math.pow(math.sin(a / 2), 2) + math.cos(radLat1) * math.cos(radLat2) * math.pow(math.sin(b / 2), 2)))
        s = s* EARTH_RADIUS
        s*=1000
        #s = math.round(s * 10000) / 10000;
        return s;
    
    */

}