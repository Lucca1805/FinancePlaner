using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using FinanceLiquidityManager.Handler.Insurance;
using Microsoft.AspNetCore.Authorization;
using FinanceLiquidityManager.Handler.Credit;

namespace FinanceLiquidityManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InsuranceController : ControllerBase
    {
        private readonly InsuranceHandler _insurance;

        public InsuranceController(InsuranceHandler insuranceHandler)
        {
            _insurance = insuranceHandler;

        }
        [HttpPost("user/insurance")]
        [Authorize]
        public async Task<ActionResult> AddInsurance([FromBody] InsuranceModelRequest newInsurance, [FromForm] IFormFile polizze)
        {
            var userId = User.FindFirstValue("UserId");
            return await _insurance.AddInsurance(userId, newInsurance, polizze);
        }


        [HttpGet("user/insurance/{insuranceId}")]
        [Authorize]
        public async Task<ActionResult> GetOneInsurance(int insuranceId)
        {
            var userId = User.FindFirstValue("UserId");

            var currency = User.FindFirstValue("CurrencyPreference");
            return await _insurance.GetOneInsurance(userId, insuranceId,currency);
        }

        [HttpGet("user/insurances")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InsuranceResponse>>> GetAllInsuranceForUser()
        {
            var userId = User.FindFirstValue("UserId");

            var currency = User.FindFirstValue("CurrencyPreference");
            return await _insurance.GetAllInsuranceForUser(userId,currency);
        }

        [HttpDelete("user/insurance/{insuranceId}")]
        [Authorize]
        public async Task<ActionResult> DeleteOneInsurance(int insuranceId)
        {
            var userId = User.FindFirstValue("UserId");
            return await _insurance.DeleteOneInsurance(userId, insuranceId);
        }

        [HttpPut("user/insurance/{insuranceId}")]
        [Authorize]
        public async Task<ActionResult> UpdateOneInsurance(int insuranceId, [FromBody] InsuranceModel updatedInsurance)
        {
            var userId = User.FindFirstValue("UserId");
            return await _insurance.UpdateOneInsurance(userId, insuranceId, updatedInsurance);
        }

        [HttpGet("user/insurance/costhistorychart")]
        [Authorize]
        public async Task<ActionResult> getCostHistoryChartData()
        {
            var userId = User.FindFirstValue("UserId");
            var currency = User.FindFirstValue("CurrencyPreference");
            return await _insurance.getCostHistoryData(userId,currency);
        }

        [HttpGet("user/insurance/intervallChart")]
        [Authorize]
        public async Task<ActionResult> GetIntervallChart()
        {
            var userId = User.FindFirstValue("UserId");
            var currency = User.FindFirstValue("CurrencyPreference");
            return await _insurance.GetIntervallChart(userId,currency);
        }

    }
}