// Copyright (c) Nathan Ford. All rights reserved. ToastViewModel.cs

namespace Leaf2Google.Models.Generic
{
    public class ToastViewModel
    {
        public string Title { get; set; }

        public string Message { get; set; }

        public string ClientId { get; set; }

        public string Colour { get; set; } = "primary";
    }
}