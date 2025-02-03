using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartVault.Program.OAuth
{
    public class OAuthConfiguration
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string AuthorizationEndpoint { get; set; }

        public string TokenEndpoint { get; set; }

        public string RedirectUri { get; set; }

        public string[] Scopes { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public OAuthConfiguration(string clientId, string clientSecret, string authorizationEndpoint, string tokenEndpoint, string redirectUri, string[] scopes)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
            RedirectUri = redirectUri;
            Scopes = scopes;
        }

        public string GetAuthorizationUrl()
        {
            var scopeString = string.Join(" ", Scopes);
            return $"{AuthorizationEndpoint}?client_id={ClientId}&redirect_uri={RedirectUri}&response_type=code&scope={scopeString}";
        }

        public string GetTokenRequestBody(string authorizationCode)
        {
            return $"client_id={ClientId}&client_secret={ClientSecret}&code={authorizationCode}&redirect_uri={RedirectUri}&grant_type=authorization_code";
        }

        public string GetRefreshTokenRequestBody()
        {
            return $"client_id={ClientId}&client_secret={ClientSecret}&refresh_token={RefreshToken}&grant_type=refresh_token";
        }
    }
}
