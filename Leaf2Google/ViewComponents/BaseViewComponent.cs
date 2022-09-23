﻿// Copyright (c) Nathan Ford. All rights reserved. Class.cs

using Castle.Core.Internal;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Models.Car;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents
{
    public abstract class BaseViewComponent : ViewComponent
    {
        public BaseViewComponent()
        {

        }

        public bool RegisterViewComponentScript(string scriptPath)
        {
            var scripts = (HttpContext.Items["ComponentScripts"] is HashSet<string>) ? (HttpContext.Items["ComponentScripts"] as HashSet<string>) : new HashSet<string>();

            var success = scripts.Add(scriptPath);

            HttpContext.Items["ComponentScripts"] = scripts;

            return success;
        }
    }
}
