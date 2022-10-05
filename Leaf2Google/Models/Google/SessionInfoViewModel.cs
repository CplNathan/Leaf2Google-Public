// Copyright (c) Nathan Ford. All rights reserved. SessionInfoModel.cs

namespace Leaf2Google.Models.Google;

public class SessionInfoViewModel
{
    public List<AuthModel> auths { get; set; } = new();
}