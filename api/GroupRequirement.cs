using Microsoft.AspNetCore.Authorization;
namespace api
{
    class GroupRequirement : IAuthorizationRequirement
    {
        public const string PolicyName = "GroupPolicy";
    }
}
