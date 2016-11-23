using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;

namespace Bilin3d {
    public class PageNotFoundHandler : DefaultViewRenderer, IStatusCodeHandler {
        public PageNotFoundHandler(IViewFactory factory)
            : base(factory) {
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context) {
            return statusCode == HttpStatusCode.NotFound;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context) {
            var response = RenderView(context, "Home/404");
            response.StatusCode = HttpStatusCode.NotFound;
            context.Response = response;
        }
    }
}