using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api
{
    class GroupPolicyHandler : AuthorizationHandler<GroupRequirement>
    {
        private readonly OktaSettings _okta;
        private readonly ILogger<GroupPolicyHandler> _logger;

        public GroupPolicyHandler(IOptions<OktaSettings> okta, ILogger<GroupPolicyHandler> logger)
        {
            _okta = okta.Value;
            _logger = logger;
        }
        const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupRequirement requirement)
        {
            _logger.LogInformation($"In GroupPolicyHandler with {context.User.Claims.Count()} claims");
            _logger.LogInformation($"   Okta Issuer is {_okta.Issuer}");
            foreach (var c in context.User.Claims)
            {
                if (c.Type == ScopeClaimType) {
                    _logger.LogInformation($"   scope: {c.Value}");
                } else if (c.Type == "clients") {
                    _logger.LogInformation($"   {c.ToString()}");
                }
            }

            var client = context.User.Claims.SingleOrDefault(c => c.Type == ScopeClaimType && c.Value.StartsWith(_okta.ScopePrefix))?.Value;
            if (client != null)
            {
                /*
                    Examples
                    _okta.ScopePrefix = "casualty.datacapture.client"
                    _okta.GroupPrefix = "CCC-DataCapture-Client"
                    scope = casualty.datacapture.client.usaa
                    group = CCC-DataCapture-Client-USAA-Group
                */
                var toMatch = $"{_okta.GroupPrefix}-{client.Replace(_okta.ScopePrefix,"").Trim('.').Replace('.','-')}-group".ToLowerInvariant();
                if (context.User.Claims.Any( c => c.Type == "clients"
                                             && c.Value.ToLowerInvariant() == toMatch))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning($"Didn't find group match for scope {client} ({toMatch})");
                }
            }
            else
            {
                _logger.LogWarning($"Didn't get only one scope with prefix {_okta.ScopePrefix}");
            }

            return Task.CompletedTask;
        }
    }
}
