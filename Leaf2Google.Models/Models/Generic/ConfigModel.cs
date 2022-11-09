// Copyright (c) Nathan Ford. All rights reserved. ConfigModel.cs

using System;

namespace Leaf2Google.Models.Generic
{

    public class ConfigModel : BaseModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}