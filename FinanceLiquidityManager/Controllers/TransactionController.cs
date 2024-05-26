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
using Microsoft.AspNetCore.Authorization;

namespace LoginController.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionHandler _transaction;

        public TransactionController(TransactionHandler transactionHandler)
        {
            _transaction = transactionHandler;

        }

        [HttpPost("user/transactions")]
        [Authorize]
        public async Task<ActionResult> GetAllTransactions(TransactionModelRequest request)
        {
            var userId = User.FindFirstValue("UserId");
            return await _transaction.GetAllTransactions(userId,request);
        }
    }
}
