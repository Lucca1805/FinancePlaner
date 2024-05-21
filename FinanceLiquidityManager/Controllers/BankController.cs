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
using FinanceLiquidityManager.Infrastructure.Bank;

namespace FinanceLiquidityManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BankController : ControllerBase
    {
        private readonly BankHandler _bank;

        public BankController(BankHandler bank)
        {
            _bank = bank;

        }
        
        [HttpGet("user/bank/{bankId}")]
        public async Task<ActionResult<BankModel>> GetOneBank(int bankId)
        {
            return await _bank.GetOneBank(bankId);
        }
    }
}