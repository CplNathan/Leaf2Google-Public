// Copyright (c) Nathan Ford. All rights reserved. ToastController.cs

using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API
{
    [Route("api/[controller]/[action]/{id?}")]
    [ApiController]
    public class ToastController : BaseController
    {
        public ToastController(ILogger<HomeController> logger, LeafSessionManager sessions)
        : base(logger, sessions)
        {
        }

        [HttpPost]
        public IActionResult Create([FromBody] ToastViewModel toast)
        {
            return PartialView("Toaster/ToastPartial", toast);
        }
    }
}
