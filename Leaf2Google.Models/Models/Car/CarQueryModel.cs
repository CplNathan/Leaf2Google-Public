// Copyright (c) Nathan Ford. All rights reserved. CarQueryModel.cs

using System;
using System.Collections.Generic;
using System.Text;

public enum QueryType
{
    PrimaryVin,
    Battery,
    Climate,
    Location,
    Photo,
    None
}

namespace Leaf2Google.Models.Car
{
    public class QueryResponse
    {
    }

    public class QueryRequest
    {
        public QueryType QueryType { get; set; } = QueryType.None;

        public string? ActiveVin { get; set; }
    }
}
