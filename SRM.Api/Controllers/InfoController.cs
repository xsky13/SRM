using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SRM.Api.Config;

namespace SRM.Api.Controllers
{
    public class InfoController(IOptions<AppOptions> _appOptions) : ControllerBase
    {
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var info = new
            {
                ServiceName = _appOptions.Value.ServiceName,
                Environment = _appOptions.Value.EnvironmentLabel,
                Timestamp = DateTime.UtcNow,
            };
            return Ok(info);
        }
    }
}
