// Copyright (c) Nathan Ford. All rights reserved. BaseAPIController.cs

using Leaf2Google.Blazor.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API;

// Instances of this type get picked up by reflection and rendered on the frontend as an endpoint object.
public class BaseAPIController : BaseController
{
    protected ICarSessionManager SessionManager { get; }

    public BaseAPIController(BaseStorageService storageManager, ICarSessionManager sessionManager)
        : base(storageManager)
    {
        SessionManager = sessionManager;
    }
}