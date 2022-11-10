// Copyright (c) Nathan Ford. All rights reserved. ConfigModel.cs

using System;
using Leaf2Google.Models;

namespace Leaf2Google.Entities.Generic
{

    public class ConfigModel : BaseModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}