using System;
using System.Linq;
using Nancy;
using Nancy.Extensions;
using Bilin3d.Models;
using System.Data;
using log4net;
using ServiceStack.OrmLite;
using System.Text;
using System.Text.RegularExpressions;
using Nancy.Security;
using System.IO;
using WxPayAPI;
using ThoughtWorks.QRCode.Codec;
using System.Drawing;
using System.Drawing.Imaging;

namespace Bilin3d.Modules {
    public class OrderModule : BaseModule {
        public OrderModule(IDbConnection db, ILog log, IRootPathProvider pathProvider)
            : base("/order") {

            this.RequiresAuthentication();

            Get["/"] = parameters => {
                string stateid = Request.Query["state"].Value;
                if (stateid != null) {
                    if (!Regex.IsMatch(stateid, @"^[1-9]\d*?$")) {
                        throw new Exception("stateid只能是大于0的数字");
                        //return null;
                    }
                }

                string condition = " and 1=1 ";
                if (stateid != null) {
                    condition = string.Format(@" and t3.Id='{0}' ", stateid);
                }

                var orderStates = db.Select<OrderStateModel>(string.Format(@"
                    select id,statename from t_orderstate"));
                var orders = db.Select<OrderModel>(string.Format(@"
                    select t1.OrderId,
                        t2.Express,
                        t1.CreateTime,
                        t1.Amount,
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
                    where t1.UserId='{0}' {1}
                    order by t1.EditTime desc", Page.UserId, condition))
                    //.GroupBy(i => new { i.OrderId, i.CreateTime, i.Consignee, i.StateName })
                    .GroupBy(i => i.OrderId)
                    .ToDictionary(k => k.Key, v => v.ToList());

                base.Page.Title = "我的订单";
                base.Model.OrderStates = orderStates;
                base.Model.Orders = orders; 
                return View["Index", base.Model];
            };

            Post["/"] = parameters => {
                string addressid = Request.Form.addressid;
                string payid = Request.Form.payid;
                string remark = Request.Form.remark;

                string province = db.Select<string>(@"
                    select Province from t_address where Id=@addressid", new {addressid = addressid}).FirstOrDefault();
                if (province == null) {
                    base.Page.Errors.Add(new ErrorModel() {Name = "", ErrorMessage = "收货地址不能为空"});
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                string pay = db.Select<string>(@"
                    select name from t_pay where Id=@payid", new {payid = payid}).FirstOrDefault();
                if (pay == null) {
                    base.Page.Errors.Add(new ErrorModel() {Name = "", ErrorMessage = "支付方式不能为空"});
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                decimal kd = 22;
                if (province.Contains("福建")) {
                    kd = 13;
                }

                string stateId = "1";
                string orderid = Page.UserId + DateTime.Now.ToString("yyyyMMddhhmmssffff");
                string sql = string.Format(@"
                        INSERT INTO t_order (OrderId,UserId, Amount, AddressId, Remark, StateId,EditTime, CreateTime)
                        SELECT '{0}',UserId, Amount+{2}, '{3}','{4}','{5}',NOW(), NOW()
                        FROM T_Car
                        WHERE UserId='{1}';

                        INSERT INTO t_orderdetail (OrderId,FileName,Weight,Area,Size,Volume,Num,MaterialId,Price,Amount,EditTime,CreateTime,SupplierId)
                        SELECT '{0}',FileName,Weight,Area,Size,Volume,Num,MaterialId,Price,Amount,NOW(),NOW(),SupplierId
                        FROM T_CarDetail
                        WHERE CarId=(SELECT CarId FROM T_Car WHERE UserId='{1}');

                        delete FROM T_CarDetail WHERE CarId=(SELECT CarId FROM T_Car WHERE UserId='{1}');
                        delete from T_Car WHERE UserId='{1}';
                    ", orderid, Page.UserId, kd, addressid, remark, stateId);
                db.ExecuteNonQuery(sql);
                return Response.AsJson(new {
                    message = "success",
                    orderId = orderid
                });
            };

            Get["/{id}"] = parameters => {
                string id = parameters.id;
                var orders = db.Select<OrderModel>($@"
                    select t1.OrderId,
                        t1.Express,
                        t1.CreateTime,
                        t1.Amount,
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
                        t4.Address 
                    from t_order  t1
                    left join t_orderdetail  t2 on t2.OrderId=t1.OrderId
                    left join t_orderstate   t3 on t3.Id=t1.StateId
                    left join t_address  t4 on t4.Id=t1.AddressId
                    left join t_material  t5 on t5.MaterialId=t2.MaterialId
                    where t1.UserId='{Page.UserId}' and t2.OrderId=@OrderId
                    order by t1.EditTime desc", new { OrderId = id });
                base.Page.Title = "订详明细";
                base.Model.Orders = orders;
                return View["Detail", base.Model];
            };

            Get["qrcode/{orderId}"] = parameters => {
                string orderId = parameters.orderId;
                var order = db.Single<OrderModel>("select OrderId,Amount from t_order where OrderId=@OrderId and UserId=@UserId and StateId=1", new { UserId = Page.UserId, OrderId = orderId });
                var response = new Response();
                response.ContentType = "image/png";
                response.Contents = stream => {
                    using (var writer = new BinaryWriter(stream)) {
                        writer.Write(
                            qrcode(
                                order.OrderId,
                                "3D打印订单付款",
                                (order.Amount * 100).ToString("f0")
                            )
                        );
                    }
                };
                return response;
            };

            Get["pay/{orderId}"] = parameters => {
                Page.Title = "付款";
                var orderId = parameters.orderId;
                var amount = db.Single<string>("select amount from t_order where OrderId=@OrderId and UserId=@UserId and StateId=1", new { UserId = Page.UserId, OrderId = orderId });
                if (amount == null) {                    
                    return "订单号出错, 可能是订单号已完成付款，或订单号不存在!";
                }

                Model.orderId = orderId;
                Model.amount = decimal.Parse(amount).ToString("f2");
                return View["Pay", base.Model];
            };

            Get["notice/{orderId}"] = parameters => {
                var orderId = parameters.orderId;
                var stateId = db.Single<string>("select StateId from t_order where OrderId=@OrderId and UserId=@UserId", new { UserId = Page.UserId, OrderId = orderId });
                if (stateId == "2") {
                    return Response.AsJson(new { message = "success" });
                }
                return Response.AsJson(new { message = "error" });
            };

            Get["paysuccess"] = _ => {
                base.Page.Title = "付款成功！";
                return View["PaySuccess", base.Model];
            };

        }

        public class NativeNotifyModule : BaseModule {
            public NativeNotifyModule(IDbConnection db, ILog log, IRootPathProvider pathProvider) {

                Post["/WxPayNotify"] = parameters => {
                    NativeNotify nativeNatify = new NativeNotify(Context);
                    var result = nativeNatify.ProcessNotify();
                    if (result.Item1 == false) {
                        log.Error($"发生错误了:{result.Item2.ToJson()}");
                        throw new Exception(result.Item2.ToXml());
                    }

                    string orderId = result.Item2.GetValue("out_trade_no").ToString().Trim();
                    string sql = $"select 1 from t_order where OrderId='{orderId}' and StateId=1";
                    //var _order = db.Single<string>("select 1 from t_order where OrderId=@OrderId and StateId=1", new { OrderId = orderId });
                    var _order = db.Single<string>(sql);
                    if (_order != null) {
                        //System.IO.File.WriteAllText($"gggg{DateTime.Now.ToString("yyyyMMddHHmmssss")}.txt", _order);
                        //DateTime payTime;
                        //DateTime.TryParseExact(result.Item2.GetValue("time_end").ToString(), "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out payTime);
                        db.ExecuteNonQuery($@"update t_order set StateId=2, PayFrom=1, PayOrderId='{result.Item2.GetValue("transaction_id")}', 
                                                PayTime='{result.Item2.GetValue("time_end")}', EditTime=NOW()
                                            where OrderId=@OrderId;
                                            update t_user set Expense=Expense+(select Amount from t_order where OrderId=@OrderId),
                                                PointRemain=PointRemain+(select Amount from t_order where OrderId=@OrderId),
                                                PointTotal=PointTotal+(select Amount from t_order where OrderId=@OrderId) where Id=(select UserId from t_order where OrderId=@OrderId);"
                                            , new { OrderId = orderId });
                    }
                    return null;
                };

            }
        }

        public static byte[] qrcode(string orderId,string body,string total_fee) {
            NativePay nativePay = new NativePay();

            //生成扫码支付模式二url
            string url = nativePay.GetPayUrl(orderId, body, total_fee);

            //string str = HttpUtility.UrlEncode(url);
            string str = url;

            //初始化二维码生成工具
            QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
            qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
            qrCodeEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;
            qrCodeEncoder.QRCodeVersion = 0;
            qrCodeEncoder.QRCodeScale = 4;

            //将字符串生成二维码图片
            Bitmap image = qrCodeEncoder.Encode(str, Encoding.Default);

            //保存为PNG到内存流  
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);

            //输出二维码图片
            //Response.BinaryWrite(ms.GetBuffer());
            //Response.End();

            return ms.GetBuffer();
            //return ms;         
        }
    }
}