using Core.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

public sealed class GoogleAuthOptions
{
    /// <summary>The OAuth client id Google issues the ID token's "aud" claim for.</summary>
    public required string ClientId { get; set; }
}

public sealed class GoogleTokenValidator(IOptions<GoogleAuthOptions> options) : IGoogleTokenValidator
{
    public async Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [options.Value.ClientId],
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleTokenPayload(payload.Subject, payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
