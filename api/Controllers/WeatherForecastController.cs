using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly OktaSettings _okta;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IOptions<OktaSettings> okta)
        {
            _logger = logger;
            _okta = okta.Value;
        }

        [HttpGet]
        [Authorize(Policy = GroupRequirement.PolicyName)]
        public IEnumerable<WeatherForecast> Get()
        {
            var client = Request.HttpContext.User.Claims.SingleOrDefault( c => c.Value.StartsWith(_okta.ScopePrefix))?.Value;

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = $"{Summaries[rng.Next(Summaries.Length)]} for {client?.Split('.').Last()}"
            })
            .ToArray();
        }

        [HttpGet()]
        [Route("weather")]
        public IEnumerable<WeatherForecast> GetWithoutAuth()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
