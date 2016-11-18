﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Security;

namespace Bilin3d {
    public class UserIdentity : IUserIdentity {
        public IEnumerable<string> Claims { get; set; }
 
        public string UserName { get; set; }     

    }
}