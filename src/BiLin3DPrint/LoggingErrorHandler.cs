using System;
using log4net;
using Nancy;
using Nancy.ErrorHandling;

namespace Bilin3d {
   
    public class LoggingErrorHandler : IStatusCodeHandler {
        private readonly ILog _logger = LogManager.GetLogger(typeof(LoggingErrorHandler));
        public void Handle(HttpStatusCode statusCode, NancyContext context) {
            object errorObject;
            context.Items.TryGetValue(NancyEngine.ERROR_EXCEPTION, out errorObject);
            var error = (errorObject as Exception).InnerException;

            System.Threading.ThreadPool.QueueUserWorkItem((i) => {
                _logger.Error("发生错误:" + error.Message + "\r\n" + error.StackTrace + "\r\n" + error.Source);
            });
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context) {          
            // 开发环境下，如果试图发生错误时，不会显示错误信息，所以用域名来判断是生产环境或开发环境;
            if (context.Request.Url.HostName == "www.3dworks.cn") {
                //不会显示错误信息，会执行上面的Handle方法
                return statusCode == HttpStatusCode.InternalServerError;
            } else {
                //会显示错误信息，不会执行上面的Handle方法
                return false;
            }            
        }
    }

}