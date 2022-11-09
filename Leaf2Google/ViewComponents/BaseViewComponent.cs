// Copyright (c) Nathan Ford. All rights reserved. Class.cs

using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents;

public abstract class BaseViewComponent : ViewComponent
{
    public BaseViewComponent(BaseStorageManager storageManager)
    {
        StorageManager = storageManager;
    }

    protected BaseStorageManager StorageManager { get; }

    public bool RegisterViewComponentScript(string scriptPath)
    {
        var scripts = HttpContext.Items["ComponentScripts"] is HashSet<string>
            ? HttpContext.Items["ComponentScripts"] as HashSet<string>
            : new HashSet<string>();

        var success = scripts?.Add(scriptPath);

        HttpContext.Items["ComponentScripts"] = scripts;

        return success ?? false;
    }
}