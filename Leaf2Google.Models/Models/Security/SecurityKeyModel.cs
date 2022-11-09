// Copyright (c) Nathan Ford. All rights reserved. SecurityKeyModel.cs

using System;
using Fido2NetLib.Objects;

namespace Leaf2Google.Models.Security
{
    public class StoredCredential
    {
        public byte[] UserId { get; set; }
        public PublicKeyCredentialDescriptor Descriptor { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] UserHandle { get; set; }
        public uint SignatureCounter { get; set; }
        public string CredType { get; set; }
        public DateTime RegDate { get; set; }
        public Guid AaGuid { get; set; }
    }

    public class StoredCredentialModel : StoredCredential
    {
        public byte[] CredentialId { get; set; }
    }
}