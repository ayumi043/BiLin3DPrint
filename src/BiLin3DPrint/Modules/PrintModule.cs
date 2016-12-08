using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Bilin3d.Models;
using System.Net;
using System.Data;
using log4net;
using ServiceStack.OrmLite;
using System.Text;
using System.IO;
using Yiauo.Three;
using Newtonsoft.Json.Linq;

namespace Bilin3d.Modules {
    public class PrintModule : BaseModule {
        public PrintModule(IDbConnection db, ILog log, IRootPathProvider pathProvider) : base("/print") {

            Get["/"] = parameters => {
                base.Page.Title = "3D打印";
                return View["Index", base.Model];
            };

            Get["/materials"] = parameters => {
                var materials = db.Select<Material>(q => q.State == 0);
                return Response.AsJson(materials);
                //return Response.AsJson(( new List<string> { "Foo","Bar","Hello","World"}).Select(i => new {
                //    message = i                    
                //}));
            };

            Post["/upload"] = parameters => {
                string uploadDirectory;
                string filepath = "";
                if (Context.CurrentUser == null) {
                    uploadDirectory = Path.Combine(pathProvider.GetRootPath(), "Content", "uploads", "temp");
                    filepath = "/Content/uploads/temp/";
                } else {
                    uploadDirectory = Path.Combine(pathProvider.GetRootPath(), "Content", "uploads", "3d");
                    filepath = "/Content/uploads/3d/";
                }

                if (!Directory.Exists(uploadDirectory)) {
                    Directory.CreateDirectory(uploadDirectory);
                }

                string _filename = "";
                string filename = "";
                List<Models.File> files = new List<Models.File>();

                foreach (var file in Request.Files) {
                    if (System.IO.Path.GetExtension(file.Name).ToLower() != ".stl") {
                        //return Response.AsJson("{\"files\":[{error:\"文件格式不是stl\"}]}",Nancy.HttpStatusCode.BadRequest);
                        files.Add(new Models.File() { error = "文件格式不是stl!" });
                        return Response.AsJson<JsonFileUpload>(new JsonFileUpload() {
                            files = files
                        });
                    }
                    if (file.Name.IndexOf("$") >= 0) {
                        files.Add(new Models.File() { error = "文件名不能包含'$'符号!" });
                        return Response.AsJson<JsonFileUpload>(new JsonFileUpload() {
                            files = files
                        });
                    }

                    //_filename = file.Name;
                    if (Context.CurrentUser == null) {
                        _filename = Session["TempUserId"].ToString() + "$" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fffff") + "$" + file.Name;
                        filename = Path.Combine(uploadDirectory, _filename);
                    } else {
                        string userid = "";
                        userid = db.Select<string>("select Id from T_User where Email=@email", new { email = base.Page.CurrentUser }).FirstOrDefault();

                        _filename = userid + "$" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fffff") + "$" + file.Name;
                        filename = Path.Combine(uploadDirectory, _filename);
                    }

                    using (FileStream fileStream = new FileStream(filename, FileMode.Create)) {
                        file.Value.CopyTo(fileStream);
                    }
                }


                STLReader stl = new STLReader(filename);
                if (stl.IsValid) {
                    //jsonfile.files= stl.Size.Length.ToString();
                    //fi.gg_width = stl.Size.Width.ToString();
                    //fi.gg_height = stl.Size.Height.ToString();
                    //stl.Surface.
                    files.Add(new Models.File() {
                        name = _filename,
                        fullname = filepath + _filename,
                        length = stl.Size.Length.ToString(),
                        width = stl.Size.Width.ToString(),
                        height = stl.Size.Height.ToString(),
                        surface = stl.Surface.ToString(),
                        volume = stl.Volume.ToString()
                    });
                } else {
                    files.Add(new Models.File() {
                        error = stl.ErrorMessage
                    });
                }
                //return Response.AsJson("{\"files\":[{name:\"" + _filename + "\"}]}", Nancy.HttpStatusCode.OK);
                //files.Add(new Models.File() { name = _filename });
                return Response.AsJson<JsonFileUpload>(new JsonFileUpload() {
                    files = files
                });

                //base.Page.Title = "上传成功";
                //return View["Index", base.Model];
            };

            Get["/suppliers"] = parameters => {
                string materialid = Request.Query["materialid"].Value;
                string _distance = Request.Query["distance"].Value;
                double distance = 20000;
                if (_distance != null) {
                    bool b = double.TryParse(_distance,out distance);
                    if (!b) distance = 20000;                    
                }                
                double lng = 0, lat = 0;

                string url = $"http://api.map.baidu.com/location/ip?ak=26904d2efeb684d7d59d493098e7295d&ip={Request.UserHostAddress}&coor=bd09ll";
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                string json = wc.DownloadString(url);
                JObject m = JObject.Parse(json);
                if (m["status"].ToString() == "0") {
                    lng = double.Parse(m["point"]["x"].ToString()); //经度
                    lat = double.Parse(m["point"]["y"].ToString()); //纬度
                }

                //lng = 118.645297;
                //lat = 24.879442;

                double range = 180 / Math.PI * distance / 6372.797; //distance代表距离，单位是km
                double lngR = range / Math.Cos(lat * Math.PI / 180.0);
                double maxLat = lat + range;
                double minLat = lat - range;
                double maxLng = lng + lngR;
                double minLng = lng - lngR;

                //暂时精确到供应商，不精确到供应商的打印机
                string sql = $@"
                    SELECT t1.supplierId,t1.fname,address,tel,qq,logo,t2.Price 
                    FROM t_supplier t1
                    join t_supplier_printer_material t2 on t2.supplierId=t1.supplierId
                    join t_printcomplete t3 on t3.completeid=t2.completeid                
                    WHERE ((lat BETWEEN '{minLat}' AND '{maxLat}') AND (lng BETWEEN '{minLng}' AND '{maxLng}'))
                        and t2.MaterialId=@MaterialId
                    Group by t1.supplierId,t1.fname,address,tel,qq,logo;";
                var suppliers = db.Select<SupplierModel>(sql, new { MaterialId = materialid });
                return Response.AsJson(suppliers.Select(i => new {
                    supplierId = i.SupplierId,
                    fname = i.Fname,
                    address = i.Address,
                    tel = i.Tel,
                    qq = i.QQ,
                    logo = i.Logo
                }));

            };
        }

    }
}