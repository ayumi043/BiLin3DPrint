using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bilin3d.Models {
    public class JsonFileUpload {
        public List<File> files { get; set; }       
    }

    public class File {
        public string name { get; set; }
        public string fullname { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string length { get; set; }
        public string surface { get; set; }
        public string volume { get; set; }
        
        public string error { get; set; }
    }
}