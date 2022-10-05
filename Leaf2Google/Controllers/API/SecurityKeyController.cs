// Copyright (c) Nathan Ford. All rights reserved. SecurityKeyController.cs

using Fido2NetLib;
using Fido2NetLib.Objects;
using Leaf2Google.Models.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Fido2NetLib.Fido2;

namespace Leaf2Google.Controllers.API;

[Route("api/[controller]/[action]/{id?}")]
[ApiController]
public class SecurityKeyController : BaseAPIController
{
    private readonly IFido2 _fido2;

    private readonly LeafContext _leafContext;
    private readonly string _origin;

    public SecurityKeyController(ICarSessionManager sessionManager, LeafContext leafContext,
        IOptions<Fido2Configuration> fido2Configuration)
        : base(sessionManager)
    {
        _origin = fido2Configuration.Value.FullyQualifiedOrigins.FirstOrDefault();

        _fido2 = new Fido2(new Fido2Configuration
        {
            ServerDomain = fido2Configuration.Value.ServerDomain,
            ServerName = fido2Configuration.Value.ServerName,
            Origins = fido2Configuration.Value.FullyQualifiedOrigins
        });

        _leafContext = leafContext;
    }

    [HttpPost]
    public JsonResult MakeCredentialOptions([FromForm] string attType, [FromForm] string authType,
        [FromForm] bool requireResidentKey, [FromForm] string userVerification)
    {
        try
        {
            if (!SessionId.HasValue)
                return Json(new CredentialCreateOptions { Status = "error", ErrorMessage = "Not logged in" });

            if (Session is null)
                return Json(new CredentialCreateOptions { Status = "error", ErrorMessage = "Not logged in" });

            var username = Session.Username;

            var user = new Fido2User
            {
                DisplayName = Session.Username,
                Name = Session.Username,
                Id = SessionId.Value.ToByteArray()
            };

            var existingKeys = _leafContext.SecurityKeys.Where(key => key.CredentialId.Length > 0)
                .Select(key => new PublicKeyCredentialDescriptor(key.CredentialId)).ToList();

            var authenticatorSelection = new AuthenticatorSelection
            {
                RequireResidentKey = requireResidentKey,
                UserVerification = userVerification.ToEnum<UserVerificationRequirement>()
            };

            if (!string.IsNullOrEmpty(authType))
                authenticatorSelection.AuthenticatorAttachment = authType.ToEnum<AuthenticatorAttachment>();

            var exts = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
                UserVerificationMethod = true
            };

            var options = _fido2.RequestNewCredential(user, existingKeys, authenticatorSelection,
                attType.ToEnum<AttestationConveyancePreference>(), exts);

            // 4. Temporarily store options, session/in-memory cache/redis/db
            HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

            //settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            return Json(options);
        }
        catch (Exception e)
        {
            return Json(new CredentialCreateOptions { Status = "error", ErrorMessage = e.Message });
        }
    }

    [HttpPost]
    public async Task<JsonResult> MakeCredential([FromBody] AuthenticatorAttestationRawResponse attestationResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
            var options = CredentialCreateOptions.FromJson(jsonOptions);

            IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
            {
                if (await _leafContext.SecurityKeys.AnyAsync(
                        key => key.CredentialId == args.CredentialId /*key.UserHandle == args.User.Id*/,
                        cancellationToken))
                    return false;

                return true;
            };

            var success = await _fido2.MakeNewCredentialAsync(attestationResponse, options, callback,
                cancellationToken: cancellationToken);

            await _leafContext.SecurityKeys.AddAsync(new StoredCredentialModel
            {
                Descriptor = new PublicKeyCredentialDescriptor(success.Result.CredentialId),
                PublicKey = success.Result.PublicKey,
                UserHandle = success.Result.User.Id,
                SignatureCounter = success.Result.Counter,
                CredType = success.Result.CredType,
                RegDate = DateTime.Now,
                AaGuid = success.Result.Aaguid,
                UserId = SessionId.Value.ToByteArray(),
                CredentialId = success.Result.CredentialId
            });

            success.Result.AttestationCertificate = null;
            success.Result.AttestationCertificateChain = null;

            await _leafContext.SaveChangesAsync();

            return Json(success);
        }
        catch (Exception e)
        {
            return Json(new CredentialMakeResult("error", e.Message, null));
        }
    }

    [HttpPost]
    public async Task<JsonResult> MakeAssertionOptions([FromForm] string? username, [FromForm] string? userVerification)
    {
        try
        {
            //var leafs = _leafContext.NissanLeafs();
            var credentials = await _leafContext.SecurityKeys.ToListAsync();
            var existingCredentials = credentials
                .Where(key => key.CredentialId.Length > 0)
                .Where(key => _leafContext.NissanLeafs.Any(car =>
                    car.NissanUsername == username && new Guid(key.UserId) == car.CarModelId))
                .Select(key => new PublicKeyCredentialDescriptor(key.CredentialId)).ToList();

            var exts = new AuthenticationExtensionsClientInputs
            {
                UserVerificationMethod = true
            };

            var uv = string.IsNullOrEmpty(userVerification)
                ? UserVerificationRequirement.Discouraged
                : userVerification.ToEnum<UserVerificationRequirement>();
            var options = _fido2.GetAssertionOptions(
                existingCredentials,
                uv,
                exts
            );

            HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());

            return Json(options);
        }

        catch (Exception e)
        {
            return Json(new AssertionOptions { Status = "error", ErrorMessage = e.Message });
        }
    }

    [HttpPost]
    public async Task<JsonResult> MakeAssertion([FromBody] AuthenticatorAssertionRawResponse clientResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get the assertion options we sent the client
            var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
            var options = AssertionOptions.FromJson(jsonOptions);

            var securitykeys = await _leafContext.SecurityKeys.ToListAsync();
            var creds = securitykeys.Where(key => key.CredentialId.Length > 0).FirstOrDefault(key =>
                            new PublicKeyCredentialDescriptor(key.CredentialId).Id.SequenceEqual(clientResponse.Id)) ??
                        throw new Exception("Unknown credentials");

            // 3. Get credential counter from database
            var storedCounter = creds.SignatureCounter;

            // 4. Create callback to check if userhandle owns the credentialId
            IsUserHandleOwnerOfCredentialIdAsync callback = async (args, cancellationToken) =>
            {
                var storedCreds =
                    await _leafContext.SecurityKeys.FirstOrDefaultAsync(key => key.UserHandle == args.UserHandle,
                        cancellationToken);
                return new PublicKeyCredentialDescriptor(storedCreds.CredentialId).Id.SequenceEqual(args.CredentialId);
            };

            var res = await _fido2.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter, callback,
                cancellationToken: cancellationToken);

            var key = await _leafContext.SecurityKeys.FirstOrDefaultAsync(key => key.CredentialId == res.CredentialId);
            if (key != null)
            {
                key.SignatureCounter = res.Counter;
                _leafContext.SecurityKeys.Update(key);
                await _leafContext.SaveChangesAsync();

                var car = await _leafContext.NissanLeafs.FirstOrDefaultAsync(car =>
                    car.CarModelId == new Guid(key.UserId));
                SessionId = car?.CarModelId ?? Guid.Empty;
            }

            // 7. return OK to client
            return Json(res);
        }
        catch (Exception e)
        {
            return Json(new AssertionVerificationResult { Status = "error", ErrorMessage = e.Message });
        }
    }
}