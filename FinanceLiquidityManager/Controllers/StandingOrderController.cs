
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using FinanceLiquidityManager.Handler.StandingOrder;

namespace FinanceLiquidityManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StandingOrderController : ControllerBase
    {

        private readonly StandingOrderHandler _standingOrder;

        public StandingOrderController(StandingOrderHandler standingOrder)
        {
            _standingOrder = standingOrder;

        }
        

        [HttpGet("user/allstandingOrders")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<StandingOrder>>> GetAllStandingOrders()
        {
            var userId = User.FindFirstValue("UserId");
            return await _standingOrder.GetAllStandingordersForUser(userId);
        }

        [HttpGet("user/standingOrdersByTime")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<StandingOrder>>> GetAllStandingordersForUserByTime([FromQuery] DateTime dateTime)
        {
            var userId = User.FindFirstValue("UserId");
            return await _standingOrder.GetAllStandingordersForUserByTime(userId,dateTime);
        }
    }


}