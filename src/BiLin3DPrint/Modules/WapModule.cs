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
            : base("/hub") {

            Get["/"] = parameters => {
                base.Page.Title = "首页";
                //log.Error("首页");
                return View["Index", base.Model];
            };

            Get["/about"] = parameters => {
                base.Page.Title = "关于我们";

                return View["About", base.Model];
            };

            Get["/faq"] = parameters => {
                base.Page.Title = "使用说明";

                return View["Faq", base.Model];
            };

            Get["/wiki"] = parameters => {
                base.Page.Title = "知识教育";

                return View["Wiki", base.Model];
            };

            Get["/help"] = parameters => {
                base.Page.Title = "帮助中心";

                return View["About", base.Model];
            };          

            Get["/hubs.js"] = _ => {
                //string str = @"var data_info = [
                //    [116.417854, 39.921988, '地址：北京市东城区王府井大街88号乐天银泰百货八层'],
                //    [116.406605, 39.921585, '地址：北京市东城区东华门大街'],
                //    [116.412222, 39.912345, '地址：北京市东城区正义路甲5号'],
                //    [118.038, 25.472, '地址：福建省xx市有限公司'],
                //    [94.29, 35.747, '地址：福建省xx市有限公司']
                //];";
                string str = "var data_info = [";
                var suppliers = db.Select<SupplierModel>("SELECT Lng,Lat,Fname FROM t_supplier WHERE State='0' and (Lng <> 0 or Lng <> 0)");
                suppliers.ForEach(i => {
                    str = str + $@"[{i.Lng},{i.Lat},'{i.Fname}'],";
                });
                str = str.TrimEnd(',') + "];";
                return Response.AsText(str);
            };

            //Get["/MP_verify_TDETj8nUoOHLCnHe.txt"] = parameters => {
            //    return Response.AsText(System.IO.File.ReadAllText(pathProvider.GetRootPath() + "MP_verify_TDETj8nUoOHLCnHe.txt"));
            //};
                        
        }
    }
}