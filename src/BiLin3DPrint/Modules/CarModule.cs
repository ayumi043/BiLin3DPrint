using System;
using System.Linq;
using Nancy;
using Nancy.Validation;
using Nancy.ModelBinding;
using Bilin3d.Models;
using System.Data;
using log4net;
using ServiceStack.OrmLite;
using System.Text;
using System.Text.RegularExpressions;
using Nancy.Security;

namespace Bilin3d.Modules {
    public class CartModule : BaseModule {
        public CartModule(IDbConnection db, ILog log, IRootPathProvider pathProvider)
            : base("/car") {

            Get["/"] = parameters => {
                base.Page.Title = "购物车";
                if (base.Page.IsAuthenticated) {
                    var cars = db.Select<CarModel>(string.Format(@"
                        select t1.amount,
                            t1.CarId as CarId,
                            t2.Id as CarDetailId,
                            t2.amount as AmountDetail,
                            t2.filename,
                            t2.area,
                            t2.size,
                            t2.num,
                            t2.weight,
                            t2.price,
                            t3.price1,
                            t3.name as MatName, 
                            t3.Color as MatColor,
                            t3.Accuracy as Accuracy,
                            t3.Density as MatDensity,
                            t3.Delivery as Delivery
                        from T_Car t1
                        left join T_CarDetail t2 on t2.CarId=t1.CarId
                        left join T_Material t3 on t3.MaterialId=t2.MaterialId
                        where t1.UserId=(SELECT Id FROM T_User WHERE Email='{0}');
                        ", Page.CurrentUser));
                    base.Model.Cars = cars;
                } else {
                    var cars = db.Select<CarModel>(string.Format(@"
                        select t1.amount,
                            t1.CarId as CarId,
                            t2.Id as CarDetailId,
                            t2.amount as AmountDetail,
                            t2.filename,
                            t2.area,
                            t2.size,
                            t2.num,
                            t2.weight,
                            t2.price,
                            t3.price1,
                            t3.name as MatName, 
                            t3.Color as MatColor,
                            t3.Accuracy as Accuracy,
                            t3.Delivery as Delivery
                        from T_CarTemp t1
                        left join T_CarDetailTemp t2 on t2.CarId=t1.CarId
                        left join T_Material t3 on t3.MaterialId=t2.MaterialId
                        where t1.UserId='{0}';
                        ", Session["TempUserId"].ToString()));
                    base.Model.Cars = cars;
                }
                return View["CheckCar", base.Model];
            };

            Post["/"] = parameters => {
                var cardetailids = Request.Form.cardetailid;
                string[] nums = Request.Form.num.Value.Split(',');
                string[] ids = cardetailids.Value.Split(',');
                string carid = Request.Form.carid;

                #region 验证 nums，ids，carid是否都存在或合法
                if (nums.All(i => Regex.IsMatch(i, @"^[1-9]\d*?$")) == false) {  //数量只能是大于0的数字
                    base.Page.Errors.Add(new ErrorModel() { Name = "数量", ErrorMessage = "数量只能是大于0的数字" });
                    //return View["CheckCar", base.Model];
                }

                string table_CarDetail = "";
                if (Page.IsAuthenticated) {
                    table_CarDetail = "T_CarDetail";
                } else {
                    table_CarDetail = "T_CarDetailTemp";
                }
                var ids_details = db.Select<string>(string.Format(@"
                    select id from {0} where carid='{1}'", table_CarDetail, carid));
                if (ids_details == null) {
                    base.Page.Errors.Add(new ErrorModel() { Name = "购物车", ErrorMessage = "购物车不存在" });
                    //return View["CheckCar", base.Model];
                }

                if (ids.All(i => ids_details.Contains(i)) == false) {
                    base.Page.Errors.Add(new ErrorModel() { Name = "购物车", ErrorMessage = "购物车子id不存在" });
                    //return View["CheckCar", base.Model];
                }

                if (base.Page.Errors.Count > 0) {
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }
                #endregion


                StringBuilder sql = new StringBuilder();
                if (Page.IsAuthenticated) {
                    var index = 0;
                    foreach (var s in nums) {
                        sql.Append(string.Format(@"
                        update T_CarDetail t1 set 
                                Num={0},
                                EditTime=NOW(),
                                Amount = (
	                                SELECT SUM(t1.Price * {0})
	                                FROM T_Material mat where mat.MaterialId = t1.MaterialId                                
                                )
                        where Id='{1}';", s, ids[index]));
                        index++;
                    }
                    sql.Append(string.Format(@"
                                update T_Car 
                                set EditTime=NOW(), 
                                Amount=(select SUM(Amount) from T_CarDetail where CarId='{0}') 
                                where carid='{0}'", carid));

                } else {
                    var index = 0;
                    foreach (var s in nums) {
                        sql.Append(string.Format(@"
                        update T_CarDetailTemp t1 set 
                                Num={0},
                                EditTime=NOW(),
                                Amount = (
	                                SELECT SUM(t1.Price * {0})
	                                FROM T_Material mat where mat.Id = t1.MaterialId                                
                                )
                        where Id='{1}';", s, ids[index]));
                        index++;
                    }
                    sql.Append(string.Format(@"
                                update T_CarTemp 
                                set EditTime=NOW(), 
                                    Amount=(select SUM(Amount) from T_CarDetailTemp where CarId='{0}') 
                                where CarId='{0}'", carid));
                }
                db.ExecuteNonQuery(sql.ToString());

                //log.Error(cardetailids);
                //return Response.AsRedirect("/account/checkout");
                return null;
            };

            Get["/checkout"] = parameters => {
                this.RequiresAuthentication();
                base.Page.Title = "结账";
                var cars = db.Select<CarModel>(string.Format(@"
                        select t1.amount,
                            t1.id as CarId,
                            t2.Id as CarDetailId,
                            t2.amount as AmountDetail,
                            t2.filename,
                            t2.area,
                            t2.size,
                            t2.volume,
                            t2.num,
                            t2.weight,
                            t2.price,
                            t3.price1,
                            t3.name as MatName, 
                            t3.Color as MatColor,
                            t3.Accuracy as Accuracy,
                            t3.Density as MatDensity,
                            t3.Delivery as Delivery
                        from T_Car t1
                        left join T_CarDetail t2 on t2.CarId=t1.CarId
                        left join T_Material t3 on t3.MaterialId=t2.MaterialId
                        where t1.UserId='{0}';
                        ", Page.UserId));

                var addresses = db.Select<AddressModel>(string.Format(@"
                        select *
                        from t_address
                        where userid='{0}'
                        ", Page.UserId));

                base.Model.Cars = cars;
                base.Model.Addresses = addresses;
                return View["CheckOut", base.Model];
            };

            Post["/add"] = parameters => {
                //string matid = Request.Form.matid;
                //string filename = Request.Form.filename;                
                //string area = Request.Form.Area;
                var model = this.Bind<CarModel>();
                var result = this.Validate(model);
                if (!result.IsValid) {
                    foreach (var item in result.Errors) {
                        foreach (var member in item.Value) {
                            base.Page.Errors.Add(new ErrorModel() { Name = item.Key, ErrorMessage = member.ErrorMessage });
                        }
                    }
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                if (base.Page.IsAuthenticated) {
                    //Mysql返回主表自增ID: SELECT LAST_INSERT_ID();
                    //                    string sql = string.Format(@"        
                    //                            SELECT Id FROM T_Car WHERE UserId IN(SELECT Id FROM T_User WHERE Email='{0}');
                    //                            ", Page.CurrentUser);
                    string sql = "SELECT CarId FROM T_Car WHERE UserId =@UserId;";
                    var carid = db.Select<string>(sql, new { UserId = Page.UserId }).FirstOrDefault();
                    if (carid == null) {  //购物车主表没记录
                        carid = Guid.NewGuid().ToString("N");
                        sql = string.Format(@"
                            INSERT INTO T_Car(UserId,Amount,EditTime,CarId) VALUES('{0}','',NOW(),'{1}');
                            INSERT INTO T_CarDetail (
	                            CarId,
	                            FileName,
	                            Volume,
	                            Area,
	                            Size,
	                            Num,
	                            MaterialId,
	                            Price,
                                Weight,
	                            EditTime,
                                SupplierId
                            )
                            VALUES
	                            (
		                            '{1}',
		                            '{2}',                
		                            '{3}',                       
		                            '{4}',                   
		                            '{5}',
		                            '{6}',
		                            '{7}',
		                            (SELECT Price FROM T_Material WHERE MaterialId={7}),
                                    (SELECT Density*{3} FROM T_Material WHERE MaterialId={7}),
		                            NOW(),
                                    '{8}'
	                            );
                            
                            ", base.Page.UserId, carid, model.FileName, model.Volume, model.Area, model.Size, model.Num, model.MaterialId,model.SupplierId);
                    } else {
                        sql = string.Format(@"select 1 from T_CarDetail where MaterialId={0} and FileName='{1}'", model.MaterialId, model.FileName);
                        var materialid = db.Select<string>(sql).FirstOrDefault();
                        if (materialid == null) {
                            sql = string.Format(@"
                            INSERT INTO T_CarDetail (
	                            CarId,
	                            FileName,
                                Volume,
	                            Area,
	                            Size,
	                            Num,
	                            MaterialId,
	                            Price,
                                Weight,
	                            EditTime,
                                SupplierId
                            )
                            VALUES
	                            (
		                            '{0}',
		                            '{1}',                
		                            '{2}',                       
		                            '{3}',                   
		                            '{4}',
		                            '{5}',
		                            '{6}',
		                            (SELECT Price FROM T_Material WHERE MaterialId={6}),
                                    (SELECT Density*{2} FROM T_Material WHERE MaterialId={6}),
		                            NOW(),
                                    '{7}'
	                            );                            
                            ", carid, model.FileName, model.Volume, model.Area, model.Size, model.Num, model.MaterialId,model.SupplierId);
                        } else {
                            sql = string.Format(@"update T_CarDetail set EditTime=NOW(), Num={0} where MaterialId={1} and FileName='{2}';", model.Num, model.MaterialId, model.FileName);
                        }

                    }

                    //更新更新子表金额Amount字段，不足最低接单价格的按最低接单价格算
                    //更新主表总金额Amount字段
                    sql = sql + string.Format(@"
                       UPDATE T_CarDetail t1
                       SET EditTime = NOW(),
                           Amount = (
	                            SELECT SUM(t1.Price * t1.Num) 
	                            FROM T_Material mat 
                                where mat.MaterialId = t1.MaterialId                                
                           )
                        WHERE t1.CarId='{0}';

                        update T_Car set EditTime=NOW(), Amount=(select SUM(Amount) from T_CarDetail where CarId='{0}') where CarId='{0}'
                        ", carid);
                    var num = db.ExecuteNonQuery(sql);
                } else {  // 未登陆
                    string sql = string.Format(@"SELECT CarId FROM T_CarTemp WHERE UserId='{0}';", Session["TempUserId"].ToString());
                    var carid = db.Select<string>(sql).FirstOrDefault();
                    if (carid == null) {  //临时购物车主表没记录
                        carid = Guid.NewGuid().ToString("N");
                        sql = string.Format(@"
                            INSERT INTO T_CarTemp(UserId,Amount,EditTime,CarId) VALUES('{0}','',NOW(),'{1}');
                            INSERT INTO T_CarDetailTemp (
	                            CarId,
	                            FileName,
                                Volume,
	                            Area,
	                            Size,
	                            Num,
	                            MaterialId,
	                            Price,
                                Weight,
	                            EditTime,
                                SupplierId
                            )
                            VALUES
	                            (
		                            '{1}',
		                            '{2}',                
		                            '{3}',                       
		                            '{4}',                   
		                            '{5}',
		                            '{6}',
		                            '{7}',
		                            (SELECT Price FROM T_Material WHERE Id={7}),
                                    (SELECT Density*{3} FROM T_Material WHERE Id={7}),
		                            NOW(),
                                    '{8}'
	                            );                            
                            ", Session["TempUserId"].ToString(), carid, model.FileName, model.Volume, model.Area, model.Size, model.Num, model.MaterialId,model.SupplierId);
                    } else {
                        sql = string.Format(@"select 1 from T_CarDetailTemp where MaterialId={0} and FileName='{1}'", model.MaterialId, model.FileName);
                        var materialid = db.Select<string>(sql).FirstOrDefault();
                        if (materialid == null) {
                            sql = string.Format(@"
                            INSERT INTO T_CarDetailTemp (
	                            CarId,
	                            FileName,
                                Volume,
	                            Area,
	                            Size,
	                            Num,
	                            MaterialId,
	                            Price,
                                Weight,
	                            EditTime
                            )
                            VALUES
	                            (
		                            '{0}',
		                            '{1}',                
		                            '{2}',                       
		                            '{3}',                   
		                            '{4}',
		                            '{5}',
		                            '{6}',
		                            (SELECT Price FROM T_Material WHERE MaterialId={6}),
                                    (SELECT Density*{2} FROM T_Material WHERE MaterialId={6}),
		                            NOW()
	                            );                            
                            ", carid, model.FileName, model.Volume, model.Area, model.Size, model.Num, model.MaterialId);
                        } else {
                            sql = string.Format(@"update T_CarDetailTemp set Num={0},EditTime=NOW() where MaterialId={1} and FileName='{2}';", model.Num, model.MaterialId, model.FileName);
                        }
                    }

                    //更新更新子表金额Amount字段，不足最低接单价格的按最低接单价格算
                    //更新主表总金额Amount字段
                    sql = sql + string.Format(@"
                        UPDATE T_CarDetailTemp t1
                        SET EditTime = NOW(),
                            Amount = (
	                            SELECT SUM(t1.Price * t1.Num)
	                            FROM T_Material mat where mat.MaterialId = t1.MaterialId                                
                            )
                        WHERE t1.CarId='{0}';

                        update T_CarTemp set EditTime=NOW(), Amount=(select SUM(Amount) from T_CarDetailTemp where CarId='{0}') where CarId='{0}'
                        ", carid);
                    var num = db.ExecuteNonQuery(sql);

                    Session["CarAdded"] = "1"; // 表示已加入临时购物车了， 用于在登录/注册成功后，转换成正式用户购物车时判断（ConvertTempCar），避免一次数据库操作；
                }
                return null;
                //return Response.AsJson(new { result = "success" });
            };

            //获取顶部的购物车的商品
            Get["/get"] = parameters => {
                if (base.Page.IsAuthenticated) {
                    var cars = db.Select<CarModel>(@"
                        select t1.amount,
                            t1.CarId as CarId,
                            t2.Id as CarDetailId,
                            t2.filename,
                            t2.area,
                            t2.size,
                            t2.num,
                            t2.price,
                            t3.name as MatName, 
                            t3.Color as MatColor,
                            t3.Accuracy as Accuracy,
                            t3.Delivery as Delivery
                        from T_Car t1
                        left join T_CarDetail t2 on t2.CarId=t1.CarId
                        left join T_Material t3 on t3.MaterialId=t2.MaterialId
                        where t1.UserId=(SELECT Id FROM T_User WHERE Email=@Email);
                        ", new { Email = Page.CurrentUser });
                    return Response.AsJson(cars.Select(i => new {
                        i.Accuracy,
                        i.Amount,
                        i.Num,
                        i.Price,
                        i.Size,
                        i.MatColor,
                        i.MatName,
                        i.Delivery,
                        i.CarDetailId,
                        i.CarId,
                        FileName = i.FileName.Split('$').Last()
                    }));

                } else {
                    //log.Debug(Session["TempUserId"]);
                    var cars = db.Select<CarModel>(@"
                        select t1.amount,
                            t1.CarId as CarId,
                            t2.Id as CarDetailId,
                            t2.filename,
                            t2.area,
                            t2.size,
                            t2.num,
                            t2.price,
                            t3.name as MatName, 
                            t3.Color as MatColor,
                            t3.Accuracy as Accuracy,
                            t3.Delivery as Delivery
                        from T_CarTemp t1
                        left join T_CarDetailTemp t2 on t2.CarId=t1.CarId
                        left join T_Material t3 on t3.MaterialId=t2.MaterialId
                        where t1.UserId=@UserId;
                        ", new { UserId = Session["TempUserId"].ToString() });
                    return Response.AsJson(cars.Select(i => new {
                        i.Accuracy,
                        i.Amount,
                        i.Num,
                        i.Price,
                        i.Size,
                        i.MatColor,
                        i.MatName,
                        i.Delivery,
                        i.CarDetailId,
                        i.CarId,
                        FileName = i.FileName.Split('$').Last()
                    }));
                }
            };

            //删除购物车里的商品
            Post["/material/del/{id}-{carid}"] = parameters => {
                string id = parameters.id;
                string carid = parameters.carid;
                CarCount carcount;
                if (Page.IsAuthenticated) {
                    // 第二条是删除子表没记录时的sql
                    db.ExecuteNonQuery(@"
                        delete from T_CarDetail where id=@Id;
                        delete from T_Car where carid not in (select carid from T_CarDetail);
                        update T_Car set EditTime=NOW(), Amount=(select SUM(Amount) from T_CarDetail where CarId=@CarId) where CarId=@CarId;
                        ", new { Id = id, CarId = carid });
                    carcount = db.Select<CarCount>(@"                                             
                        select sum(t2.amount) as SumAmount,
                            sum(t2.num) as SumNum
                        from T_Car t1
                        join T_CarDetail t2 on t2.CarId=t1.CarId
                        where t1.UserId=@UserId;
                        ", new { UserId = Page.UserId }).FirstOrDefault();
                } else {
                    // 第二条是删除子表没记录时的sql
                    db.ExecuteNonQuery(@"
                        delete from T_CarDetailTemp where id=@Id;
                        delete from T_Car where carid not in (select carid from T_CarDetail);
                        update T_CarTemp set EditTime=NOW(), Amount=(select SUM(Amount) from T_CarDetailTemp where CarId=@CarId) where CarId=@CarId;
                        ", new { Id = id, CarId = carid });
                    carcount = db.Select<CarCount>(@"                                             
                        select sum(t2.amount) as SumAmount,
                            sum(t2.num) as SumNum
                        from T_CarTemp t1
                        join T_CarDetailTemp t2 on t2.CarId=t1.CarId
                        where t1.UserId=@UserId;
                        ", new { UserId = Session["TempUserId"].ToString() }).FirstOrDefault();
                }
                //return Response.AsJson(new { result = "success: " + parameters.id + "!" });
                return Response.AsJson(carcount);
            };

        }

    }
}