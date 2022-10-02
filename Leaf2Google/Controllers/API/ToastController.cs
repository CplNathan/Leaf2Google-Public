// Copyright (c) Nathan Ford. All rights reserved. ToastController.cs

using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Car;
using Leaf2Google.Models.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API
{
    [Route("api/[controller]/[action]/{id?}")]
    [ApiController]
    public class ToastController : BaseAPIController
    {
        public ToastController(ICarSessionManager sessionManager)
        : base(sessionManager)
        {
        }

        [HttpPost]
        public IActionResult Create([FromBody] ToastViewModel toast)
        {
            return PartialView("Toaster/ToastPartial", toast);
        }
    }
}