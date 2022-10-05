// Copyright (c) Nathan Ford. All rights reserved. BaseAPIController.cs

namespace Leaf2Google.Controllers.API;

// Instances of this type get picked up by reflection and rendered on the frontend as an endpoint object.
public class BaseAPIController : BaseController
{
    public BaseAPIController(ICarSessionManager sessionManager)
        : base(sessionManager)
    {
    }
}