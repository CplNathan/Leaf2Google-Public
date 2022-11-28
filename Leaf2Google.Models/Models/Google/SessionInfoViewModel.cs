// Copyright (c) Nathan Ford. All rights reserved. SessionInfoModel.cs

using System.Collections.Generic;
using Leaf2Google.Entities.Google;

namespace Leaf2Google.Models.Google
{

    public class SessionInfoViewModel
    {
        public List<AuthEntity> auths { get; set; }
    }
}