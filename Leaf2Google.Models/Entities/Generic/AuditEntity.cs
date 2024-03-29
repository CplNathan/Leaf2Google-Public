﻿// Copyright (c) Nathan Ford. All rights reserved. AuditEntity.cs

using Leaf2Google.Models;
using System;

namespace Leaf2Google.Entities.Generic
{

    public enum AuditContext
    {
        Leaf,
        Google,
        Account
    }

    public enum AuditAction
    {
        Access,
        Execute,
        Delete,
        Update,
        Create,
        Modify,
        Exception
    }

    public class AuditEntity : BaseModel
    {
        public Guid Id { get; set; }

        public Guid? Owner { get; set; }

        public AuditContext Context { get; set; }

        public AuditAction Action { get; set; }

        public string? Data { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;
    }
}