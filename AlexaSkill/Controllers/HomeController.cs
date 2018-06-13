using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Abstractions;
using System.Net.Http;
using AlexaSkill;
using System.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using System.IO;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using AlexaSkill.Models;
using Newtonsoft.Json;

namespace AlexaSkill.Controllers
{
    public class HomeController : Controller
    {
        private readonly StateMachine _stateMachine;

        public HomeController()
        {
            _stateMachine = new StateMachine();
        }

        [Route("api/Sensor")]
        [HttpPost]
        public async Task<IActionResult> Sensor()
        {
            var speechlet = new DotNetApplet(_stateMachine);
            var message = Request.HttpContext.GetHttpRequestMessage();
            var response = await speechlet.GetResponseAsync(message);
            return Ok(await response.Content.ReadAsStreamAsync());
        }

        [Route("api/Sensor/Set/{userId}/{sensorController}/{sensor}/{state}")]
        [HttpPost]
        public IActionResult SetSensorState(string userId, string sensorController, string sensor, bool state)
        {
            var result = _stateMachine.SetSensorState(userId, sensorController, sensor, state ? StateValues.On : StateValues.Off);
            return Ok(new { success = result != null, sensorState = result });
        }

        [Authorize]
        public IActionResult Index()
        {
            ViewData["Name"] = User.FindFirst(ClaimTypes.Name).Value;
            ViewData["ExternalId"] = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return View();
        }
    }
}