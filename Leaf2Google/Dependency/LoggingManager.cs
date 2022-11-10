﻿// Copyright (c) Nathan Ford. All rights reserved. LoggingManager.cs

using Leaf2Google.Entities.Generic;

namespace Leaf2Google.Dependency;

public class LoggingManager : IDisposable
{
    protected readonly IServiceScopeFactory _serviceScopeFactory;

    public LoggingManager(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    private IServiceScopeFactory ServiceScopeFactory => _serviceScopeFactory;

    public async Task<string> AddLog(Guid owner, AuditAction action, AuditContext context, string data)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var nissanContext = scope.ServiceProvider.GetRequiredService<LeafContext>();

            var audit = new AuditModel
            {
                Owner = owner,
                Action = action,
                Context = context,
                Data = $"{owner} - {data}"
            };

            await nissanContext.NissanAudits.AddAsync(audit);

            return $"{audit.Owner} - {audit.Action.ToString()} - {audit.Context.ToString()} - {data}";
        }
    }

    public async void Dispose()
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var nissanContext = scope.ServiceProvider.GetRequiredService<LeafContext>();
            await nissanContext.SaveChangesAsync();
        }
    }
}