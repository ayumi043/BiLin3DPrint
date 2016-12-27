using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Bilin3d.Models
{
    public class OrderModel {
        public string OrderId { get; set; }
        public string OrderDetailId { get; set; }
        public string StateId { get; set; }
        public DateTime CreateTime { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountDetail { get; set; }
        public string Num { get; set; }

        public string Area { get; set; }
        public string Size { get; set; }
        public string Volume { get; set; }
        public string Weight { get; set; }
        public string FileName { get; set; }
        public string MatName { get; set; }
        public string StateName { get; set; }
        public string Consignee { get; set; }
        public string Express { get; set; }
        public string Address { get; set; }
        public string SupplierName { get; set; }    
        
        public int PayFrom { get; set; }
        public string payName { get; set; }
        public string PayOrderId { get; set; }
        public DateTime PayTime { get; set; }
    }


    public class OrderStateModel {
        public int Id { get; set; }
        public string StateName { get; set; }

    }
}