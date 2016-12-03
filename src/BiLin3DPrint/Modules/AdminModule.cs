using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Security;
using Bilin3d.Models;
using System.Data;
using ServiceStack.OrmLite;
using log4net;

using Nancy.Validation;
using System.Text.RegularExpressions;

namespace Bilin3d.Modules {
    public class AdminModule : BaseModule {
        public AdminModule(IDbConnection db, ILog log)
            : base("/bilinadmin") {

            Before += ctx => {
                if (Session["adminid"] == null) {
                    //return null;
                    //return Response.AsRedirect("/bilinadminlogin");
                }
                return null;
            };

            Get["/"] = parameters => {
                var users = db.Select<UserModel>("select * from T_User");
                base.Page.Title = "Home";

                return View["Admin/Index", Model];
            };

            Get["/users"] = parameters => {
                string email = Request.Query["email"];
                string strWhere = " 1=1 ";
                if (email != null) {
                    //strWhere += $" and email like '%{email}%' ";
                    strWhere += $" and email like @email ";
                }
                //var users = db.Select<UserModel>($"select * from T_User where {strWhere}");
                var users = db.Select<UserModel>($"select * from T_User where {strWhere}", new { email = "%" + email + "%", id = "" });
                base.Page.Title = "用户";

                Model.Users = users;
                return View["Admin/User/Index", Model];
            };

            Post["/user/{id}-{state}"] = parameters => {
                var id = parameters.id;
                string state = parameters.state;
                List<string> states = new List<string> { "0", "1", "2" };
                if (states.IndexOf(state) < 0) {
                    return Response.AsJson(new { message = "error" }, HttpStatusCode.BadRequest);
                }
                db.ExecuteNonQuery($"update t_user set state='{state}' where id={id};");
                return Response.AsJson(new { message = "success" });
            };

            Get["/suppliers"] = parameters => {
                string email = Request.Query["email"];
                string strWhere = " 1=1 ";
                if (email != null) {
                    strWhere += $" and email like'%{email}%'";
                }
                var users = db.Select<UserModel>($"select * from T_Supplier where {strWhere}");
                base.Page.Title = "用户";

                Model.Users = users;
                return View["Admin/User/Index", Model];
            };

            Post["/supplier/{id}-{state}"] = parameters => {
                var id = parameters.id;
                string state = parameters.state;
                List<string> states = new List<string> { "0", "1", "2" };
                if (states.IndexOf(state) < 0) {
                    return Response.AsJson(new { message = "error" }, HttpStatusCode.BadRequest);
                }
                db.ExecuteNonQuery($"update t_user set state='{state}' where id={id};");
                return Response.AsJson(new { message = "success" });
            };

            Get["/printer"] = parameters => {
                Page.Title = "打印机";
                return View["Admin/Printer/Index", Model];
            };

            Post["/printer"] = parameters => {
                return null;
            };

            Get["/printer/brand"] = parameters => {
                return null;
            };

            Post["/printer/brand"] = parameters => {
                return null;
            };

            Get["/order"] = parameters => {
                string stateid = Request.Query["state"].Value;
                if (stateid != null) {
                    if (!Regex.IsMatch(stateid, @"^[1-9]\d*?$")) {  //数量只能是大于0的数字
                        return null;
                    }
                }

                string condition = " 1=1 ";
                if (stateid != null) {
                    condition += string.Format(@" and t3.Id='{0}' ", stateid);
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
                        t4.Address 
                    from t_order  t1
                    left join t_orderdetail  t2 on t2.OrderId=t1.OrderId
                    left join t_orderstate   t3 on t3.Id=t1.StateId
                    left join t_address  t4 on t4.Id=t1.AddressId
                    left join t_material  t5 on t5.Id=t2.MaterialId
                    where {0}
                    order by t1.CreateTime desc", condition))
                                          //.GroupBy(i => new { i.OrderId, i.CreateTime, i.Consignee, i.StateName })
                                          .GroupBy(i => i.OrderId)
                                          .ToDictionary(k => k.Key, v => v.ToList());
                base.Page.Title = "所有订单";
                base.Model.OrderStates = orderStates;
                base.Model.Orders = orders;
                //return View["Admin/Order", base.Model];
                return View["Admin/Order", base.Model];
            };

        }
    }

    public class AdminLoginModule : BaseModule {
        public AdminLoginModule(IDbConnection db, ILog log, IRootPathProvider pathProvider) {
                        
            Get["/bilinadminlogin"] = parameters => {
                base.Page.Title = "管理员后台";
                return View["Admin/Login", base.Model];
            };

            Post["/bilinadminlogin"] = parameters => {
                Session["adminid"] = "abc";
                return Response.AsRedirect("/bilinadmin/");
            };

        }
    }
}