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
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using ThoughtWorks.QRCode.Codec;
using System.Text;

namespace Bilin3d.Modules {
    public class WapModule : BaseModule {
        public WapModule(IDbConnection db, ILog log, IRootPathProvider pathProvider) 
            : base("/m") {

            Get("/", parameters => {
                return null;
            });
            
            Get("/order",  _ => {
                return Response.AsRedirect($"https://open.weixin.qq.com/connect/oauth2/authorize?appid={WxPayConfig.APPID}&redirect_uri=http%3A%2F%2Fwww.3dworks.cn%2Fm%2Fmyorder&response_type=code&scope=snsapi_base&state=123#wechat_redirect");
            });

            Get("/myorder", _ => {
                string code = Request.Query["code"];
                string url = $"https://api.weixin.qq.com/sns/oauth2/access_token?appid={WxPayConfig.APPID}&secret={WxPayConfig.APPSECRET}&code={code.Trim()}&grant_type=authorization_code";

                //WebClient wc = new WebClient();                
                //string json = wc.DownloadString(url);

                //这里容易超时，用WebClient不能设置超时
                //ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebRequest request = WebRequest.Create(url);               
                request.Timeout = 1000 * 90;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                string json = "";
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)){
                    json = reader.ReadToEnd();
                    reader.Close();
                }                    

                JObject m = JObject.Parse(json);
                if (m["errcode"] != null) {
                    throw new System.Exception($"获取微信openid发生错误;json:{json};code:{code}");
                }
                string openid = m["openid"].ToString();
                var userid = db.Single<string>($@"select id from t_user where WxOpenid='{openid}';");
                if (string.IsNullOrEmpty(userid) == true) {
                    userid = "";
                }

                Model.Openid = openid;
                Model.Userid = userid;
                Session["userid"] = userid;

                //Model.Openid = "";
                //Model.Userid = "";
                //Session["userid"] = "";
                return View["Wap/Myorder", base.Model];
            });

            Post("/bindaccountwx/{openid}", parameters => {
                if (Session["userid"] != null) {
                    string openid = parameters.openid;
                    string email = Request.Form["username"];
                    string password = Request.Form["pwd"];
                    int i = db.ExecuteNonQuery($@"update t_user set WxOpenid='{openid}' where Email=@Email and PassWord=@PassWord;", new { Email = email, PassWord = password });
                    if (i > 0) {
                        string userid = db.Single<string>($@"select id  from t_user where WxOpenid='{openid}'", new { Email = email, PassWord = password });
                        return Response.AsJson(new { message = "success", userid = userid });
                    }                    
                }
                return Response.AsJson(new { message = "error" }, Nancy.HttpStatusCode.BadRequest);
            });

            Get("/order/{userid}",parameters => {
                if (Session["userid"] != null) {
                    string userid = parameters.userid;
                    var orders = db.Select<OrderModel>($@"
                    select t1.OrderId,
                        t2.Express,
                        t1.CreateTime,
                        t1.Amount,
                        t2.Amount as AmountDetail,                       
                        t2.Area,
                        t2.Size,
                        t2.Volume,
                        t2.Weight,
                        t2.FileName,
                        t2.Num,
                        t5.name as MatName,
                        t3.StateName,
                        t3.Id as StateId,
                        t4.Consignee,
                        t4.Address,
                        t6.Fname as SupplierName 
                    from t_order  t1
                    left join t_orderdetail  t2 on t2.OrderId=t1.OrderId
                    left join t_orderstate   t3 on t3.Id=t1.StateId
                    left join t_address  t4 on t4.Id=t1.AddressId
                    left join t_material  t5 on t5.MaterialId=t2.MaterialId
                    left join t_supplier t6 on t6.SupplierId=t2.SupplierId
                    where t1.UserId=@UserId
                    order by t1.EditTime desc", new { UserId = userid })
                        .GroupBy(i => i.StateId)
                        .ToDictionary(k => k.Key, v => v.ToList());
                    var _orders = orders.Select(i => {
                        var tmp = i.Value.GroupBy(x => x.OrderId).ToDictionary(y => y.Key, y => y.ToList());
                        return new {
                            state = i.Key,
                            orders = tmp.Select(m => new { orderid = m.Key, detail = m.Value })
                        };
                    });
                    //string json = JsonConvert.SerializeObject(_orders);  //orders.json
                    return Response.AsJson(_orders);
                }
                return Response.AsJson(new { message = "error" }, Nancy.HttpStatusCode.BadRequest);
            });

            Get("/qrcode/{orderId}", parameters => {
                if (Session["userid"] != null) {
                    string orderId = parameters.orderId;
                    var order = db.Single<OrderModel>("select OrderId,Amount from t_order where OrderId=@OrderId and StateId=1", new { OrderId = orderId });
                    var response = new Response();
                    response.ContentType = "image/png";
                    response.Contents = stream => {
                        using (var writer = new BinaryWriter(stream)) {
                            writer.Write(
                                OrderModule.qrcode(
                                    order.OrderId,
                                    "3D打印订单付款",
                                    (order.Amount * 100).ToString("f0")
                                )
                            );
                        }
                    };
                    return response;
                }
                return null;                
            });

            Get("/succ1", _ => {
                base.Page.Title = "比邻3d订单查询";
                string wxOpenid = "";
                //string wxAccount = "";
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
                    return Response.AsJson(new { message = "您的账户还没关联，请先关联!" }, Nancy.HttpStatusCode.BadRequest);
                }

                string rr = "";
                using (SqlDataReader dr = Lib.SqlServerHelper.ExecuteReader(constr, CommandType.Text, sql)) {
                    if (dr.Read()) {
                        rr = dr[0].ToString();
                    }
                }

                return View["Wap/order", base.Model];
            });

        }
    }
}