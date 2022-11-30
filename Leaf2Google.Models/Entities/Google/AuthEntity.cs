// Copyright (c) Nathan Ford. All rights reserved. AuthEntity.cs

using Leaf2Google.Entities.Car;
using Leaf2Google.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leaf2Google.Entities.Google
{
    public class GoogleAuth
    {
        public string client_id { get; set; } = string.Empty;
        public Uri? redirect_uri { get; set; }
        public string state { get; set; } = string.Empty;
    }

    public class AuthEntity : BaseModel
    {
        [Key] public Guid AuthId { get; set; }

        public virtual CarEntity? Owner { get; set; }

        [Column(TypeName = "jsonb")]
        public GoogleAuth Data { get; set; }

        public Guid? AuthCode { get; set; }

        public DateTime? LastQuery { get; set; }

        public DateTime? LastExecute { get; set; }

        public DateTime? Deleted { get; set; }
    }
}