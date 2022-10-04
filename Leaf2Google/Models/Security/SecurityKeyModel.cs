// Copyright (c) Nathan Ford. All rights reserved. SecurityKeyModel.cs

using Fido2NetLib.Development;
using System.ComponentModel.DataAnnotations;

namespace Leaf2Google.Models.Security
{
    public class StoredCredentialModel : StoredCredential
    {

        public StoredCredentialModel() { }

        public byte[] CredentialId { get; set; }
    }
}
