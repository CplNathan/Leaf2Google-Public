﻿// Copyright (c) Nathan Ford. All rights reserved. LoggingService.cs

using Leaf2Google.Entities.Generic;

namespace Leaf2Google.Services;

public class LoggingService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LoggingService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    private IServiceScopeFactory ServiceScopeFactory => _serviceScopeFactory;

    public string AddLog(Guid owner, AuditAction action, AuditContext context, string data)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var nissanContext = scope.ServiceProvider.GetRequiredService<LeafContext>();

        var audit = new AuditEntity
        {
            Owner = owner,
            Action = action,
            Context = context,
            Data = $"{owner} - {data}"
        };

        nissanContext.NissanAudits.Add(audit);
        nissanContext.SaveChanges();

        return $"{audit.Owner} - {audit.Action} - {audit.Context} - {data}";
    }
}