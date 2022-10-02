using Leaf2Google.Controllers.API;
using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Car;
using Leaf2Google.Models.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using NUglify.Helpers;
using System.Reflection;
using System.Text;

namespace Leaf2Google.Controllers
{
    public class BaseController : Controller
    {
        private readonly ICarSessionManager _sessionManager;

        protected ICarSessionManager SessionManager { get => _sessionManager; }

        protected Guid? SessionId
        {
            get
            {
                var sessionGuid = HttpContext.Session.GetString("SessionId");
                Guid parsedGuid;

                bool success = Guid.TryParse(sessionGuid, out parsedGuid);

                return success ? parsedGuid : null;
            }
            set
            {
                HttpContext.Session.SetString("SessionId", value.HasValue ? value.Value.ToString() : "");
            }
        }

        protected string? SelectedVin
        {
            get
            {
                return HttpContext.Session.GetString("SelectedVin") ?? "";
            }
            set
            {
                HttpContext.Session.SetString("SelectedVin", value ?? "");
            }
        }

        public BaseController(ICarSessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public bool RegisterViewComponentScript(string scriptPath)
        {
            var scripts = (HttpContext.Items["ComponentScripts"] is HashSet<string>) ? (HttpContext.Items["ComponentScripts"] as HashSet<string>) : new HashSet<string>();

            var success = scripts?.Add(scriptPath);

            HttpContext.Items["ComponentScripts"] = scripts;

            return success ?? false;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ReloadViewBag(true);
            base.OnActionExecuting(filterContext);
        }

        protected void ReloadViewBag(bool resetToasts = false)
        {
            ViewBag.SessionId = SessionId;
            ViewBag.SelectedVin = SelectedVin;

            Assembly asm = Assembly.GetExecutingAssembly();

            var res = asm.GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type)) //filter controllers
                .SelectMany(type => type.GetMethods())
                .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) && method.IsDefined(typeof(HttpPostAttribute)) && (method.DeclaringType?.IsSubclassOf(typeof(BaseAPIController)) ?? false))
                .Select(method => Tuple.Create(method.DeclaringType?.Name.Replace("Controller", "") ?? "", method.Name))
                .Where(method => !method.Item1.IsNullOrWhiteSpace())
                .GroupBy(method => method.Item1);

            JObject endpoints = new JObject();

            foreach (var group in res)
            {
                var endpoint = new JObject();

                foreach (var item in group)
                {
                    endpoint.Add(item.Item2, Url.ActionLink(item.Item2, item.Item1));
                }

                endpoints.Add(group.Key, endpoint);
            }

            ViewBag.API = endpoints.ToString();

            if (resetToasts)
            {
                if (ViewBag.Toasts is null)
                    ViewBag.Toasts = new List<ToastViewModel>();

                ViewBag.Toasts = ((List<ToastViewModel>)ViewBag.Toasts).Where(toast => !toast.Displayed).ToList();
                ViewBag.Toasts = ((List<ToastViewModel>)ViewBag.Toasts).Select(toast => { toast.Displayed = true; return toast; }).ToList();
            }
        }

        protected void AddToast(ToastViewModel toastView)
        {
            ((List<ToastViewModel>)ViewBag.Toasts).Add(toastView);
        }
    }
}