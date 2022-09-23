using Castle.Core.Internal;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
            Sessions.VehicleSessions.Any(session => session.SessionId == SessionId && SessionId.HasValue);

        public BaseController(ILogger<HomeController> logger, LeafSessionManager sessions, IConfiguration configuration)
        {
            _logger = logger;
            _sessions = sessions;
            _configuration = configuration;
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
            ViewBag.MapBoxKey = _configuration["MapBox:api_key"];

            if (resetToasts)
                ViewBag.Toasts = new List<ToastViewModel>();
        }

        protected void AddToast(ToastViewModel toastView)
        {
            ((List<ToastViewModel>)ViewBag.Toasts).Add(toastView);
        }
    }
}