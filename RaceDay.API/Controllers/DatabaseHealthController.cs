using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/health/database")]
    public class DatabaseHealthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;


    public DatabaseHealthController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                bool connected = await _context.Database.CanConnectAsync();

                if (connected)
                {
                    return Ok(new
                    {
                        status = "Success",
                        message = "RaceDay API connected to RaceDayDB!"
                    });
                }

                return StatusCode(503, new
                {
                    status = "Failed",
                    message = "Could not connect to RaceDayDB."
                });
            }
            catch (Exception)
            {
                return StatusCode(503, new
                {
                    status = "Failed",
                    message = "Database connection failed. Check the connection string and SQL Server."
                });
            }
        }
    }


}
