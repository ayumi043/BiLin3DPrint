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
                           
            //new Task(() => {
            //    _logger.Error("发生错误啦!", error);
            //}).Start();

            System.Threading.ThreadPool.QueueUserWorkItem((i) => {
                _logger.Error("发生错误:"+error.Message +"\r\n" + error.StackTrace);
            }); 
        }
        
        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context) {
            return statusCode == HttpStatusCode.InternalServerError;
        }
    }
}