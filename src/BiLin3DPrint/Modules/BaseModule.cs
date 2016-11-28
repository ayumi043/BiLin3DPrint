using System.Collections.Generic;
using System.Linq;
using Nancy;
using System.Dynamic;
using Bilin3d.Models;
using ServiceStack.OrmLite;
using Microsoft.Extensions.Configuration;

namespace Bilin3d.Modules {
    public class BaseModule : NancyModule {

        public dynamic Model = new ExpandoObject();

        protected PageModel Page { get; set; }

        public BaseModule() {
            SetupModelDefaults();
        }

        public BaseModule(string modulepath)
            : base(modulepath) {
            SetupModelDefaults();
        }

        private void SetupModelDefaults() {
            Before += ctx => {

                var _UserId = "";
                var _IsSupplier = false;
                if (ctx.CurrentUser != null) {
                    var dbFactory = new OrmLiteConnectionFactory(BiLin3D.Startup.Configuration.GetConnectionString("mysql"), MySqlDialect.Provider);
                    var db = dbFactory.OpenDbConnection();
                    var userRecord = db.Select<UserModel>(q => q.Email == ctx.CurrentUser.UserName).FirstOrDefault();
                    _UserId = userRecord.Id.ToString();
                    if (userRecord.SupplierId != "") {
                        _IsSupplier = true;
                    }
                }

                Page = new PageModel() {
                    IsAuthenticated = ctx.CurrentUser != null,
                    PreFixTitle = "Bilin 3D - ",
                    CurrentUser = ctx.CurrentUser != null ? ctx.CurrentUser.UserName : "",
                    UserId = _UserId,
                    IsSupplier = _IsSupplier,
                    Errors = new List<ErrorModel>()
                };

                Model.Page = Page;

                return null;
            };
        }

    }
}