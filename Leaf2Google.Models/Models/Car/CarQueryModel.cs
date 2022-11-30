// Copyright (c) Nathan Ford. All rights reserved. CarQueryModel.cs

public enum QueryType
{
    PrimaryVin,
    Battery,
    Lock,
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
