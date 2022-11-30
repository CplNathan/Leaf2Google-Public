// Copyright (c) Nathan Ford. All rights reserved. CarActionModel.cs

public enum ActionType
{
    Battery,
    Climate,
    Lights,
    Horn,
    None
}

namespace Leaf2Google.Models.Car
{
    public class ActionResponse
    {

    }

    public class ActionRequest
    {
        public ActionType Action { get; set; }

        public int Duration { get; set; }
    }
}
