using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Bilin3d.Models.CustomAnnotations;

namespace Bilin3d.Models {
    public class SupplierWithCompletePriceModel {
        public string SupplierId { get; set; }
              
        public string Tel { get; set; }
        public string Fname { get; set; }

        public string QQ { get; set; }
             
        public string Address { get; set; }

        public string Logo { get; set; }

        public decimal MatPrice { get; set; }
        public string PrintCompleteName { get; set; }
        public string SupplierPrinterMaterialId { get; set; }

        

    }
       
}