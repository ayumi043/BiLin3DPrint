using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Bilin3d.Models.CustomAnnotations;

namespace Bilin3d.Models {
    public class SupplierPrinterMaterialModel {
        public string SupplierId { get; set; }
        public string PrinterId { get; set; }
        public string PrinterName { get; set; }
        public string PrinterState { get; set; }
        public string MaterialId { get; set; }
        public string MaterialName { get; set; }
    }
}