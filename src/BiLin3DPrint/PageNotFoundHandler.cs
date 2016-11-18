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
            //dynamic Model = new ExpandoObject();
            //PageModel page = new PageModel() {
            //    IsAuthenticated = context.CurrentUser != null,
            //    PreFixTitle = "Bilin 3D - ",
            //    CurrentUser = context.CurrentUser != null ? context.CurrentUser.UserName : "",
            //    Errors = new List<ErrorModel>()
            //};
            //page.Title = "404";
            //Model.Page = page;

            var response = RenderView(context, "Home/404");
            response.StatusCode = HttpStatusCode.NotFound;
            context.Response = response;
        }
    }
}