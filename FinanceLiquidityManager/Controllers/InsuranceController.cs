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

namespace LoginController.Controllers
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

        [HttpPost("GetInsurance")]
        public async Task<IActionResult> Get()
        {
            return await _insurance.Get();
        }

        
    }
}
