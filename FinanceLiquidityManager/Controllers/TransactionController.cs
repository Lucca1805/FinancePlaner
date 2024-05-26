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
using FinanceLiquidityManager.Handler.Transaction;

namespace LoginController.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionHandler transactionHandler;

        public TransactionController(TransactionHandler transaction)
        {
            transactionHandler = transaction;

        }

        [HttpPost("GetAllTransactions")]
        public async Task<IActionResult> Get()
        {
            return await transactionHandler.Get();
        }

        
    }
}
