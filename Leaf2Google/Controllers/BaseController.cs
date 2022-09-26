using Leaf2Google.Controllers.API;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;

namespace Leaf2Google.Controllers
{
    public class BaseController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly LeafSessionManager _sessions;

        private readonly IConfiguration _configuration;

        protected LeafSessionManager Sessions { get => _sessions; }

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

        public bool IsLoggedIn() =>
            Sessions.VehicleSessions.GetValueOrDefault(SessionId ?? Guid.Empty) != null;

        public BaseController(ILogger<HomeController> logger, LeafSessionManager sessions, IConfiguration configuration)
        {
            _logger = logger;
            _sessions = sessions;
            _configuration = configuration;
        }

        public bool RegisterViewComponentScript(string scriptPath)
        {
            var scripts = (HttpContext.Items["ComponentScripts"] is HashSet<string>) ? (HttpContext.Items["ComponentScripts"] as HashSet<string>) : new HashSet<string>();

            var success = scripts.Add(scriptPath);

            HttpContext.Items["ComponentScripts"] = scripts;

            return success;
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
                .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) && method.IsDefined(typeof(HttpPostAttribute)) && method.DeclaringType.IsSubclassOf(typeof(BaseAPIController)))
                .Select(method => Tuple.Create(method.DeclaringType.Name.Replace("Controller", ""), method.Name))
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
                ViewBag.Toasts = new List<ToastViewModel>();
        }

        protected void AddToast(ToastViewModel toastView)
        {
            ((List<ToastViewModel>)ViewBag.Toasts).Add(toastView);
        }
    }
}