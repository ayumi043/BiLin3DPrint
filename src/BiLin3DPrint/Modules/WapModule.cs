using Nancy;
using Bilin3d.Models;
using System.Data;
using ServiceStack.OrmLite;
using log4net;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WxPayAPI;
using System.Net;
using Newtonsoft.Json.Linq;

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
                string code = Request.Query["code"];
                string url = $"https://api.weixin.qq.com/sns/oauth2/access_token?appid={WxPayConfig.APPID}&secret={WxPayConfig.APPSECRET}&code={code}&grant_type=authorization_code";
                WebClient wc = new WebClient();
                string json = wc.DownloadString(url);
                JObject m = JObject.Parse(json);
                string openid = m["openid"].ToString();
                var userid = db.Single<string>($@"select id from t_user where WxOpenid='{openid}';");
                if (string.IsNullOrEmpty(userid) == true) {
                    userid = "";
                }

                Model.Code = code;
                Model.Openid = openid;
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
                    return Response.AsJson(new { message = "账户还没关联，请先关联!" }, Nancy.HttpStatusCode.BadRequest);
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