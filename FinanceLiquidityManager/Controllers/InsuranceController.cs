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
using FinanceLiquidityManager.Infrastructure.Insurance;
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
        public async Task<ActionResult<InsuranceModel>> GetOneInsurance(int insuranceId)
        {
            return await _insurance.GetOneInsurance(insuranceId);
        }

        [HttpGet("user/insurances/{policyHolderId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InsuranceModel>>> GetAllInsuranceForUser()
        {
            return await _insurance.GetAllInsuranceForUser();
        }

        [HttpDelete("user/insurance/{insuranceId}")]
        public async Task<ActionResult> DeleteOneInsurance(int insuranceId)
        {
            return await _insurance.DeleteOneInsurance(insuranceId);
        }

        [HttpPut("user/insurance/{insuranceId}")]
        public async Task<ActionResult> UpdateOneInsurance(int insuranceId, [FromBody] InsuranceModel updatedInsurance)
        {
            return await _insurance.UpdateOneInsurance(insuranceId, updatedInsurance);
        }
    }
}