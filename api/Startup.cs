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

        public GroupPolicyHandler(IOptions<OktaSettings> okta) {
            _okta = okta.Value;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupRequirement requirement)
        {
            WriteLine($"In GroupPolicyHandler with {context.User.Claims.Count()} claims");
            WriteLine($"   Okta Issuer is {_okta.Issuer}");
            foreach (var c in context.User.Claims)
            {
                WriteLine($"   {c.ToString()}  from {c.Issuer}");
            }

            if (context.User.HasClaim(c =>
            {
                return true;
            }))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
    public class OktaSettings
    {
        public OktaSettings()
        {}

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
