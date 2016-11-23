using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Extensions;
using Nancy.Validation;
using Nancy.ModelBinding;
using Bilin3d.Models;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;
using System.Data;
using System.Net.Mail;
using log4net;
using ServiceStack.OrmLite;
using System.Text;
using System.Text.RegularExpressions;
using Nancy.Security;
using System.IO;
using System.Threading.Tasks;
using WxPayAPI;
using Nancy.Helpers;
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
                        t4.Address,
                        t6.Fname as SupplierName 
                    from t_order  t1
                    left join t_orderdetail  t2 on t2.OrderId=t1.OrderId
                    left join t_orderstate   t3 on t3.Id=t1.StateId
                    left join t_address  t4 on t4.Id=t1.AddressId
                    left join t_material  t5 on t5.MaterialId=t2.MaterialId
                    left join t_supplier t6 on t6.SupplierId=t2.SupplierId
                    where t1.UserId='{0}' {1}
                    order by t1.CreateTime desc", Page.UserId, condition))
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
                    message = "success"
                });
            };

            Get["/{id}"] = parameters => {
                string id = parameters.id;
                var order = db.Select<OrderModel>(string.Format(@"
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
                    where t1.UserId='{0}' and t2.OrderId='{1}'
                    order by t1.CreateTime desc", Page.UserId, id)).FirstOrDefault();
                base.Page.Title = "订详明细";
                base.Model.Order = order;
                return View["Detail", base.Model];
            };


            Get["pay"] = parameters => {

                return null;
            };

            Get["qrcode"] = parameters => {

                NativePay nativePay = new NativePay();

                //生成扫码支付模式二url
                string url2 = nativePay.GetPayUrl("123456789");

                //将url生成二维码图片
                //Image2.ImageUrl = "MakeQRCode.aspx?data=" + HttpUtility.UrlEncode(url2);
                
                string str = HttpUtility.UrlEncode(url2);

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
                Response.BinaryWrite(ms.GetBuffer());
                Response.End();

                return null;
            };
            
        }

    }
}