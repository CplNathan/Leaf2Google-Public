﻿using Castle.Core.Internal;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Error()
        {
            AddToast(new ToastViewModel() { Title = "System Error", Message = $"There was an error with your last request, please try again ({HttpContext.TraceIdentifier}).", Colour = "warning" });

            ReloadViewBag();
            return await RedirectToAction("Index", "Home", ViewBag);
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
            ViewBag.MapBoxKey = _configuration["MapBox:api_key"];
            ViewBag.CaptchaKey = _configuration["Google:Captcha:site_key"];

            if (resetToasts)
                ViewBag.Toasts = new List<ToastViewModel>();
        }

        protected void AddToast(ToastViewModel toastView)
        {
            ((List<ToastViewModel>)ViewBag.Toasts).Add(toastView);
        }
    }
}