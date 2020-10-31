using System.Collections.Generic;
using System.Security.Claims;
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
using static System.Console;
namespace api
{
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
                options.AddPolicy(GroupRequirement.PolicyName, policy => policy.Requirements.Add(new GroupRequirement()));
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

            // remove from sample otherwise always get 401
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
