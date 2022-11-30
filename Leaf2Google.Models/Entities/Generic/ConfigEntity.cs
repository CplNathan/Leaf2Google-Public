// Copyright (c) Nathan Ford. All rights reserved. ConfigEntity.cs

using Leaf2Google.Models;
using System;

namespace Leaf2Google.Entities.Generic
{

    public class ConfigEntity : BaseModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}