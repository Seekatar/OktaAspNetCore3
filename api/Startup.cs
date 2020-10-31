using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Console;
namespace api
{
    class GroupRequirement : IAuthorizationRequirement
    {

    }
    class GroupPolicyHandler : AuthorizationHandler<GroupRequirement>
    {
        private readonly OktaSettings _okta;

        public GroupPolicyHandler(IOptions<OktaSettings> okta)
        {
            _okta = okta.Value;
        }
        const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupRequirement requirement)
        {
            WriteLine($"In GroupPolicyHandler with {context.User.Claims.Count()} claims");
            WriteLine($"   Okta Issuer is {_okta.Issuer}");
            foreach (var c in context.User.Claims)
            {
                if (c.Type == ScopeClaimType) {
                    WriteLine($"   scope: {c.Value}");
                } else if (c.Type == "clients") {
                    WriteLine($"   {c.ToString()}");
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
                    WriteLine($"Didn't find group match for scope {client} ({toMatch})");
                }
            }
            else
            {
                WriteLine($"Didn't get only one scope with prefix {_okta.ScopePrefix}");
            }

            return Task.CompletedTask;
        }
    }
    public class OktaSettings
    {
        public string ScopePrefix { get; set; }
        public string GroupPrefix { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public override string ToString()
        {
            return $"Iss: '{Issuer}' Aud: '{Audience}'";
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<OktaSettings>()
                     .Bind(Configuration.GetSection("Okta"))
                     .ValidateDataAnnotations();

            var okta = Configuration.GetSection("Okta").Get<OktaSettings>();
            if (okta == null)
            {
                WriteLine("Okta must be configured for service to run.");
                return;
            }
            else
            {
                WriteLine(okta.ToString());
            }

            services.AddControllers();
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    // both values from from API->Authorization Servers
                    // if don't match exactly, get 401 returned to client
                    options.Authority = okta.Issuer;
                    options.Audience = okta.Audience;
                    options.RequireHttpsMetadata = false;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("MyPolicy", policy => policy.Requirements.Add(new GroupRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, GroupPolicyHandler>();
            WriteLine("Added GroupPolicyHandler singleton");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
