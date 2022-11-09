// Copyright (c) Nathan Ford. All rights reserved. CarSelectorViewComponent.cs

using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents.Car;

public class CarSelectorViewComponent : BaseViewComponent
{
    public CarSelectorViewComponent(BaseStorageManager storageManager)
        : base(storageManager)
    {
    }

    public IViewComponentResult Invoke(Guid? sessionId)
    {
        var session = StorageManager.VehicleSessions.GetValueOrDefault(sessionId ?? Guid.Empty);

        if (session != null)
            return View(session.VINs);
        return View(new List<string?>());
    }
}