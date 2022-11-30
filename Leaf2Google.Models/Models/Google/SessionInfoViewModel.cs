// Copyright (c) Nathan Ford. All rights reserved. SessionInfoViewModel.cs

using Leaf2Google.Entities.Google;
using System.Collections.Generic;

namespace Leaf2Google.Models.Google
{

    public class SessionInfoViewModel
    {
        public List<AuthEntity> auths { get; set; }
    }
}