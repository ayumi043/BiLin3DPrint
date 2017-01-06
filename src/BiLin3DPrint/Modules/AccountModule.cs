using System;
using System.Linq;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Validation;
using Nancy.ModelBinding;
using Bilin3d.Models;
using System.Net;
using System.Data;
using System.Net.Mail;
using log4net;
using ServiceStack.OrmLite;
using System.Text;
using System.Text.RegularExpressions;
using Nancy.Security;
using System.IO;

namespace Bilin3d.Modules {
    public class AccountModule : BaseModule {
        public AccountModule(IDbConnection db, ILog log, IRootPathProvider pathProvider)
            : base("/account") {
            Get("/logon", parameters => {
                base.Page.Title = "用户登录";

                //var loginModel = new LoginModel();
                var loginModel = new LoginModel() { RememberMe = true };
                base.Model.LoginModel = loginModel;

                return View["LogOn", base.Model];
            });

            Post("/logon", parameters => {
                var model = this.Bind<LoginModel>();
                var result = this.Validate(model);

                var userMapper = new UserMapper(db);
                var userGuid = userMapper.ValidateUser(model.Email, model.Password);

                if (userGuid == null || !result.IsValid) {
                    base.Page.Title = "用户登录";

                    foreach (var item in result.Errors) {
                        foreach (var member in item.Value) {
                            base.Page.Errors.Add(new ErrorModel() { Name = item.Key, ErrorMessage = member.ErrorMessage });
                        }
                    }

                    if (userGuid == null && base.Page.Errors.Count == 0)
                        base.Page.Errors.Add(new ErrorModel() { Name = "Email", ErrorMessage = "该用户不存在或密码输入错误" });


                    base.Model.LoginModel = model;

                    return View["LogOn", base.Model];
                }

                DateTime? expiry = null;
                if (model.RememberMe) {
                    expiry = DateTime.Now.AddDays(100);
                }

                // 把临时购物车转到用户账下
                ConvertTempCar(db, pathProvider, userGuid);

                return this.LoginAndRedirect(userGuid.Value, expiry);
            });

            Get("/logoff", parameters => {
                if (Session["TempUserId"] != null) {
                    Session.Delete("TempUserId");
                }
                if (Session["CarAdded"] != null) {
                    Session.Delete("CarAdded");
                }

                return this.LogoutAndRedirect("/");
            });

            Get("/register", parameters => {
                base.Page.Title = "用户注册";

                var registerModel = new RegisterModel();
                base.Model.RegisterModel = registerModel;

                return View["Register", base.Model];
            });

            Post("/register", parameters => {
                var model = this.Bind<RegisterModel>();
                var result = this.Validate(model);

                if (!result.IsValid) {
                    base.Page.Title = "用户注册";

                    base.Model.RegisterModel = model;

                    foreach (var item in result.Errors) {
                        foreach (var member in item.Value) {
                            base.Page.Errors.Add(new ErrorModel() { Name = item.Key, ErrorMessage = member.ErrorMessage });
                        }
                    }

                    return View["Register", base.Model];
                }

                var userMapper = new UserMapper(db);
                var userGuid = userMapper.ValidateRegisterNewUser(model);

                //User already exists
                if (userGuid == null) {
                    base.Page.Title = "用户注册";
                    base.Model.RegisterModel = model;
                    base.Page.Errors.Add(new ErrorModel() { Name = "Email", ErrorMessage = "Email已经存在了" });
                    return View["Register", base.Model];
                    //return Response.AsRedirect("/account/register");                       
                }

                DateTime? expiry = DateTime.Now.AddDays(100);


                // 把临时购物车转到用户账下
                ConvertTempCar(db, pathProvider, userGuid);

                return this.LoginAndRedirect(userGuid.Value, expiry);
            });

            Get("/findpwd", parameters => {
                base.Page.Title = "密码找回";
                return View["FindPwd", base.Model];
            });

            Post("/findpwd", parameters => {
                string email = Request.Form.Email;
                if (!Regex.IsMatch(email, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$")) {
                    base.Page.Errors.Add(new ErrorModel() { Name = "", ErrorMessage = "您输入的email格式不正确!" });
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                var user = db.Select<UserModel>(q => q.Email == email).FirstOrDefault();
                if (user == null) {                   
                    base.Page.Errors.Add(new ErrorModel() { Name = "", ErrorMessage = "您输入的email不存在!" });
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                //SMTP服务器
                SmtpClient smtp = new SmtpClient("smtp.qq.com"); //需要登陆邮箱到后台开启smtp/pop3服务
                smtp.Credentials = new NetworkCredential("87308217", "lina123");

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("87308217@qq.com", "Bilin3D打印服务");   // 发件人
                mail.To.Add(email);  // 收件人
                mail.Subject = "密码找回";
                mail.Body = string.Format("<h4>你好,{0}，您的登陆密码是：{1}</h4>", email, user.PassWord);
                mail.BodyEncoding = Encoding.UTF8;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.Normal;
                try {
                    smtp.Send(mail);
                } catch (Exception ex) {
                    log.Error(ex.Message);
                    return Response.AsJson(new { message = ex.Message },Nancy.HttpStatusCode.BadRequest);
                }

                string domain = email.Split('@').ToList().Last().ToLower();
                string email_url = "";
                if (domain == "qq.com") {
                    email_url = string.Format("<a href='{0}' target='_blank'>点击前往腾讯邮箱</a>", "http://mail.qq.com/");
                }
                if (domain == "163.com") {
                    email_url = string.Format("<a href='{0}' target='_blank'>点击前往网易邮箱</a>", "http://mail.163.com/");
                }

                //base.Page.Title = "密码找回成功";
                //base.Model.Message = string.Format(@"密码已发送到您的邮箱中了，请查收!   {0}", email_url);
                //return View["FindPwdResult", base.Model];
                return Response.AsJson(new { message = string.Format(@"密码已发送到您的邮箱中了，请查收!   {0}", email_url) });
            });
                                                          
            Get("/info", parameters => {
               this.RequiresAuthentication();
                var user = db.Single<UserModel>("select * from t_user where Id=@Id", new { Id = Page.UserId });
                base.Page.Title = "个人信息";
                base.Model.User = user;
                return View["Info", base.Model];
            });

            Post("/info", parameters => {
                this.RequiresAuthentication();
                string avatars = Request.Form.avatars;
                string nickname = Request.Form.nickname;
                string tel = Request.Form.tel;
                string sql = string.Format(@"
                    UPDATE t_user
                    SET                   
                        NickName = '{0}',
                        Tel = '{1}',
                        Avatars = '{2}',                                       
                        EditTime = NOW()                    
                    WHERE
	                    Id = '{3}';", nickname, tel, avatars, Page.UserId);
                db.ExecuteNonQuery(sql);
                return Response.AsJson(new { message = "success" });
            });

            Post("/info/uploadimg", parameters => {
                this.RequiresAuthentication();
                string uploadDirectory;
                uploadDirectory = Path.Combine(pathProvider.GetRootPath(), "Content", "uploads", "avatars");
                if (!Directory.Exists(uploadDirectory)) {
                    Directory.CreateDirectory(uploadDirectory);
                }

                var file = Request.Files.First();

                if (file.Value.Length > 1024 * 1024 * 0.5) {  //不能大于0.5MB(512kb)
                    base.Page.Errors.Add(new ErrorModel() { Name = "", ErrorMessage = "文件不能大于512kb太大了" });
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                string _filename = "", filename = "";
                string[] imgs = new string[] { ".jpg", ".png", ".gif", ".bmp", ".jpeg" };
                if (!imgs.Contains(System.IO.Path.GetExtension(file.Name).ToLower())) {
                    base.Page.Errors.Add(new ErrorModel() { Name = "", ErrorMessage = "文件格式不正确" });
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }
                _filename = Page.UserId + "$" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fffff") + "$" + file.Name;
                filename = Path.Combine(uploadDirectory, _filename);
                using (FileStream fileStream = new FileStream(filename, FileMode.Create)) {
                    file.Value.CopyTo(fileStream);
                }

                return Response.AsJson(new { filename = _filename });
            });

            Get("/address", parameters => {
                this.RequiresAuthentication();
                var addresses = db.Select<AddressModel>(string.Format(@"
                        select *
                        from t_address
                        where userid='{0}'
                        ", Page.UserId));
                base.Page.Title = "收货地址管理";
                base.Model.Addresses = addresses;
                return View["Address", base.Model];
            });

            Post("/address", parameters => {
                this.RequiresAuthentication();
                string id = Request.Form.id;
                int i = db.ExecuteNonQuery(string.Format(@"
                    delete from t_address where id='{0}' and UserId='{1}' ", id, Page.UserId));
                if (i < 1)
                {
                    return Response.AsJson(new { message = "error" }, Nancy.HttpStatusCode.BadRequest);
                }
                return 200;
            });

            Get("/address/{id}", parameters => {
                this.RequiresAuthentication();
                string id = parameters.id;
                var address = db.Select<AddressModel>(string.Format(@"
                    select * from t_address where id='{0}' and UserId='{1}' ", id, Page.UserId)).FirstOrDefault();
                base.Page.Title = address.Consignee;
                base.Model.Address = address;
                return View["AddressEdit", base.Model];
            });

            Post("/address/{id}", parameters => {
                this.RequiresAuthentication();
                var model = this.Bind<AddressModel>();
                var result = this.Validate(model);

                if (!result.IsValid)
                {
                    base.Model.RegisterModel = model;

                    foreach (var item in result.Errors)
                    {
                        foreach (var member in item.Value)
                        {
                            base.Page.Errors.Add(new ErrorModel() { Name = item.Key, ErrorMessage = member.ErrorMessage });
                        }
                    }
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                string sql = "";
                if (model.State == "1")
                {
                    sql += string.Format(@"update t_address set state='0' where userid='{0}' and state='1';", Page.UserId);
                }
                sql += string.Format(@"
                    update t_address 
                    set Consignee='{0}', 
                        Province='{1}',
                        City='{2}',
                        Dist='{3}',
                        Address='{4}',
                        Tel='{5}',
                        Company='{6}',
                        State='{7}'
                    where id='{8}' and UserId='{9}';
                    ", model.Consignee, model.Province, model.City, model.Dist, model.Address,
                    model.Tel, model.Company, model.State, model.Id, Page.UserId);
                db.ExecuteNonQuery(sql);
                return 200;
            });

            Post("/address/add", parameters => {
                this.RequiresAuthentication();
                var model = this.Bind<AddressModel>();
                var result = this.Validate(model);

                if (!result.IsValid)
                {
                    base.Model.RegisterModel = model;
                    foreach (var item in result.Errors)
                    {
                        foreach (var member in item.Value)
                        {
                            base.Page.Errors.Add(new ErrorModel() { Name = item.Key, ErrorMessage = member.ErrorMessage });
                        }
                    }

                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                var count = db.Select<string>(string.Format(@"
                    select count(1) from t_address where userid='{0}'", Page.UserId)).FirstOrDefault();
                if (count != null && int.Parse(count) > 20)
                {
                    base.Page.Errors.Add(new ErrorModel() { Name = "", ErrorMessage = "收货地址不能超过20个" });
                    return Response.AsJson(base.Page.Errors, Nancy.HttpStatusCode.BadRequest);
                }

                string sql = "";
                if (model.State == "1")
                {
                    sql += string.Format(@"update t_address set state='0' where userid='{0}' and state='1';", Page.UserId);
                }
                sql += string.Format(@"
                    insert into t_address(userid,company,Consignee,tel,Province,city,dist,address,state)
                        values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}');
                ", Page.UserId, model.Company, model.Consignee, model.Tel, model.Province, model.City, model.Dist, model.Address, model.State);
                db.ExecuteNonQuery(sql);

                var address = db.Select<AddressModel>(string.Format(@"
                    select * from t_address where id=(select max(id) from t_address where userid='{0}')", Page.UserId)).FirstOrDefault();

                return Response.AsJson(address);
            });
        }

        private void ConvertTempCar(IDbConnection db, IRootPathProvider pathProvider, Guid? userGuid)
        {
            if (Session["CarAdded"] != null)
            {
                string sql = "";
                string userid = "";
                userid = db.Select<string>(string.Format(@"select Id from T_User where userGuid = '{0}'", userGuid.Value)).FirstOrDefault();

                #region 把uploads/temp目录下的临时文件复制到正式的uploads/3d下
                //string fileName = "test.txt";
                //string sourcePath = @"C:\Users\Public\TestFolder";
                //string targetPath = @"C:\Users\Public\TestFolder\SubDir";
                string sourcePath = Path.Combine(pathProvider.GetRootPath(), "Content", "uploads", "temp");
                string targetPath = Path.Combine(pathProvider.GetRootPath(), "Content", "uploads", "3d");

                // Use Path class to manipulate file and directory paths.
                //string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
                //string destFile = System.IO.Path.Combine(targetPath, fileName);

                string fileName = "";
                string destFile = "";

                if (System.IO.Directory.Exists(sourcePath))
                {
                    string[] files = System.IO.Directory.GetFiles(sourcePath, Session["TempUserId"].ToString() + "*");

                    // Copy the files and overwrite destination files if they already exist.
                    foreach (string s in files)
                    {
                        // Use static Path methods to extract only the file name from the path.
                        fileName = userid + "$" + string.Join("$", System.IO.Path.GetFileName(s).Split('$').Skip(1));

                        destFile = System.IO.Path.Combine(targetPath, fileName);
                        System.IO.File.Copy(s, destFile, true);
                    }
                }
                #endregion


                //判断用户是否在购物车中有记录, 存在就不需要写入主表只要写入子表就可以了
                var carid = db.Select<string>(string.Format(@"select CarId from T_Car where UserId = '{0}'", userid)).FirstOrDefault();
                if (carid != null)
                {
                    sql = string.Format(@"
                        INSERT INTO T_CarDetail (CarId,FileName,Weight,Area,Size,Num,MaterialId,Price,EditTime,CreateTime,Volume,SupplierId)
                        SELECT '{0}','{2}',Weight,Area,Size,Num,MaterialId,Price,EditTime,CreateTime,Volume,SupplierId
                        FROM T_CarDetailTemp
                        WHERE CarId=(SELECT CarId FROM T_CarTemp WHERE UserId='{1}');
                    ", carid, Session["TempUserId"].ToString(), fileName);
                }
                else
                {
                    carid = Guid.NewGuid().ToString("N");
                    sql = string.Format(@"
                        INSERT INTO T_Car (CarId,UserId, Amount, EditTime, CreateTime)
                        SELECT '{3}','{0}', Amount, EditTime, CreateTime
                        FROM T_CarTemp
                        WHERE UserId='{1}';

                        INSERT INTO T_CarDetail (CarId,FileName,Weight,Area,Size,Num,MaterialId,Price,EditTime,CreateTime,Volume,SupplierId)
                        SELECT '{3}','{2}',Weight,Area,Size,Num,MaterialId,Price,EditTime,CreateTime,Volume,SupplierId
                        FROM T_CarDetailTemp
                        WHERE CarId=(SELECT CarId FROM T_CarTemp WHERE UserId='{1}');
                    ", userid, Session["TempUserId"].ToString(), fileName, carid);
                }

                db.ExecuteNonQuery(sql);

                //更新更新子表金额Amount字段，不足最低接单价格的按最低接单价格算
                //更新主表总金额Amount字段
                sql = string.Format(@"
                        UPDATE T_CarDetail t1
                        SET EditTime = NOW(),
                            Amount = (
	                            SELECT t1.Price * t1.Num
	                            FROM T_Material mat where mat.MaterialId = t1.MaterialId                                
                            )
                        WHERE t1.CarId='{0}';

                        update T_Car set EditTime=NOW(), Amount=(select SUM(Amount) from T_CarDetail where CarId='{0}') where CarId='{0}'
                        ", carid);
                db.ExecuteNonQuery(sql);
            }
        }

    }
}