using Azure.Identity;
using RecordVault.Domain.Persistence;

namespace RecordVault.Infrastructure.Persistence
{
    public class AzureTokenProvider : IMicrosoftTokenProvider
    {
        private readonly string _tenantId;

        private readonly string _scope;

        public AzureTokenProvider(string tenantId, string scope)
        {
            _tenantId = tenantId;
            _scope = scope;
        }

        public string GetToken()
        {
            return GetTokenAsync().Result;
        }

        public async Task<string> GetTokenAsync()
        {
            var credential = new DefaultAzureCredential();

            var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new string[] { _scope }, tenantId: _tenantId));

            var outputToken = token.Token;

            // ADLS Gen2 requires the token to be prefixed with "Bearer "
            if (!outputToken.StartsWith("Bearer "))
            {
                outputToken = $"Bearer {outputToken}";
            }

            return outputToken;
        }
    }
}
