using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using XAUAT.EduApi.Configuration;

namespace XAUAT.EduApi.Services;

public interface IMapAdminTokenService
{
    bool IsAuthorized(HttpRequest request);
}

public class MapAdminTokenService(IOptions<MapAdminOptions> options) : IMapAdminTokenService
{
    private const string TokenHeaderName = "Token";
    private const string AuthorizationHeaderName = "Authorization";
    private const string BearerPrefix = "Bearer ";

    private readonly string _expectedToken = options.Value.Token.Trim();

    public bool IsAuthorized(HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(_expectedToken))
        {
            return false;
        }

        var requestToken = ExtractToken(request);
        if (string.IsNullOrWhiteSpace(requestToken))
        {
            return false;
        }

        return FixedTimeEquals(_expectedToken, requestToken.Trim());
    }

    private static string? ExtractToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(TokenHeaderName, out var tokenValues))
        {
            var token = tokenValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
        }

        if (request.Headers.TryGetValue(AuthorizationHeaderName, out var authorizationValues))
        {
            var authorization = authorizationValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authorization) &&
                authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return authorization[BearerPrefix.Length..];
            }
        }

        return null;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
