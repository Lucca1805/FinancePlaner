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
using FinanceLiquidityManager.Handler.BankAccount;
using Microsoft.AspNetCore.Authorization;

namespace FinanceLiquidityManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BankAccountController : ControllerBase
    {
        private readonly BankAccountHandler _BankAccount;

        public BankAccountController(BankAccountHandler bankAccountHandler)
        {
            _BankAccount = bankAccountHandler;
        }

        [HttpDelete("user/personal/banks")]
        public async Task<ActionResult> DeleteAllBankAccounts()
        {
            var userId = User.FindFirstValue("UserId");
            return await _BankAccount.DeleteAllBankAccounts(userId);
        }

        [HttpDelete("user/personal/bank/{id}")]
        public async Task<ActionResult> DeleteOneBankAccount(string AccountId)
        {
            return await _BankAccount.DeleteOneBankAccount(AccountId);
        }

        [HttpGet("user/personal/banks")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<BankAccountModel>>> GetAllBankAccountsForUser()
        {
            var userId = User.FindFirstValue("UserId");
            return await _BankAccount.GetAllBankAccountsForUser(userId);
        }

        [HttpGet("user/accounts")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<BankAccount>>> GetAllAccountsForUser()
        {
            var userId = User.FindFirstValue("UserId");
            var CurrencyPreference = User.FindFirstValue("CurrencyPreference");
            return await _BankAccount.GetAllAccountsForUser(userId,CurrencyPreference);
        }
    }
}
