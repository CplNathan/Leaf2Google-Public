// Copyright (c) Nathan Ford. All rights reserved. BaseAPIController.cs

using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Managers;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API
{
    // Instances of this type get picked up by reflection and rendered on the frontend as an endpoint object.
    public class BaseAPIController : BaseController
    {
        public BaseAPIController(ILogger<HomeController> logger, LeafSessionManager sessions, IConfiguration configuration)
        : base(logger, sessions, configuration)
        {
        }
    }
}
