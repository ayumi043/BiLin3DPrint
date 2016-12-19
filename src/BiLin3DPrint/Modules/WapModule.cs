using Nancy;
using Bilin3d.Models;
using System.Data;
using ServiceStack.OrmLite;
using log4net;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Bilin3d.Modules {
    public class WapModule : BaseModule {
        public WapModule(IDbConnection db, ILog log, IRootPathProvider pathProvider) 
            : base("/m") {

            Get["/"] = parameters => {
                return null;
            };
            
            Get["/order"] = _ => {
                base.Page.Title = "比邻3d订单查询";
                return View["Wap/order", base.Model];
            };

            Get["/success"] = _ => {
                base.Page.Title = "比邻3d订单查询";
                return View["Wap/success", base.Model];
            };

            Get["/succ1"] = _ => {
                base.Page.Title = "比邻3d订单查询";
                string wxOpenid = "";
                string wxAccount = "";
                string constr = BiLin3D.Startup.Configuration.GetConnectionString("sql");
                string sql = $@"
                    SELECT t1.[OrderID]
                        ,t1.[OrderCode]
                        ,t1.[CusCode]
                        ,t1.[PersonCode]
                        ,t1.[OrderDate]
                        ,t1.[FinishDate]
                        ,t1.[OrderMoney]
                        ,t1.[OrderRemarks]
                        ,t1.[productsname]
                        ,t1.[orderState]
                        ,t1.[PersonName]
                        ,t1.[CusName]
                        ,t1.[productstypename]
                        ,t1.[ProductsType]
                        ,t1.[OrderFile]
                        ,t1.[Filename]
                        ,t1.[sendorder]
                        ,t1.[Address]
                        ,t1.[ContactType]
                        ,t1.[ContactPerson]
	                    ,t2.WxOpenid
	                    ,t2.WxAccount
                    FROM Gy_V_OrderForm t1
                    join  Gy_Customer t2 on t2.CusCode=t1.CusCode
                    where t2.WxOpenid='{wxOpenid}'";

                sql = $"SELECT count(1) FROM Gy_Customer where WxOpenid='{wxOpenid}'";
                string result = Lib.SqlServerHelper.ExecuteScalar(constr, CommandType.Text, sql).ToString();
                if (result == "0") {
                    return Response.AsJson(new { message = "账户还没关联，请先关联!" }, HttpStatusCode.BadRequest);
                }

                string rr = "";
                using (SqlDataReader dr = Lib.SqlServerHelper.ExecuteReader(constr, CommandType.Text, sql)) {
                    if (dr.Read()) {
                        rr = dr[0].ToString();
                    }
                }

                return View["Wap/order", base.Model];
            };


        }
    }
}