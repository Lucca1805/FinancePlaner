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

        [HttpGet("user/insurance/{insuranceId}")]
        [Authorize]
        public async Task<ActionResult> GetOneInsurance(int insuranceId)
        {
            var userId = User.FindFirstValue("UserId");
            return await _insurance.GetOneInsurance(userId, insuranceId);
        }

        [HttpGet("user/insurances/{policyHolderId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InsuranceModel>>> GetAllInsuranceForUser()
        {
            var userId = User.FindFirstValue("UserId");
            return await _insurance.GetAllInsuranceForUser(userId);
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
    }
}