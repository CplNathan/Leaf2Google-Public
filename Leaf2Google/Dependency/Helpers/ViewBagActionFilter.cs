// Copyright (c) Nathan Ford. All rights reserved. ViewBagActionFilter.cs

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Leaf2Google.Dependency.Helpers
{
    public class ViewBagActionFilter : ActionFilterAttribute
    {

        public ViewBagActionFilter()
        {
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            // for razor pages
            if (context.Controller is PageModel)
            {
                var controller = context.Controller as PageModel;
                controller.ViewData.Add("Avatar", $"~/avatar/empty.png");   

                //also you have access to the httpcontext & route in controller.HttpContext & controller.RouteData
            }

            // for Razor Views
            if (context.Controller is ViewComponent)
            {
                var controller = context.Controller as ViewComponent;
                controller.ViewBag.Avatar = $"~/avatar/empty.png";

                //also you have access to the httpcontext & route in controller.HttpContext & controller.RouteData
            }

            base.OnResultExecuting(context);
        }
    }
}
