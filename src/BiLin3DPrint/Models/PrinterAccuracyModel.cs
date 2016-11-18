using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Bilin3d.Models.CustomAnnotations;

namespace Bilin3d.Models {
    public class PrinterAccuracyModel {
        public string AccuracyId { get; set; }
        public string Fname { get; set; } 
    }
}