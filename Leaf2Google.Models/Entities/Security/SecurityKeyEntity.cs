﻿// Copyright (c) Nathan Ford. All rights reserved. SecurityKeyEntity.cs

using Fido2NetLib.Objects;
using System;

namespace Leaf2Google.Entities.Security
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

    public class StoredCredentialEntity : StoredCredential
    {
        public byte[] CredentialId { get; set; }
    }
}