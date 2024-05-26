
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
using FinanceLiquidityManager.Infrastructure.Credit;

namespace FinanceLiquidityManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreditController : ControllerBase
    {

        private readonly CreditHandler _credit;

        public CreditController(CreditHandler credit)
        {
            _credit = credit;

        }
        [HttpGet("user/credit/{loanId}")]
        public async Task<ActionResult<LoanModel>> GetOneCredit(int loanId)
        {
            return await _credit.GetOneCredit(loanId);
        }

        [HttpPost("user/credit")]
        public async Task<ActionResult> AddOneCredit([FromBody] LoanModel newLoan)
        {
            return await _credit.AddOneCredit(newLoan);
        }

        [HttpDelete("user/credit/{loanId}")]
        public async Task<ActionResult> DeleteOneCredit(int loanId)
        {
            return await _credit.DeleteOneCredit(loanId);
        }

        [HttpPut("user/credit/{loanId}")]
        public async Task<ActionResult> UpdateOneCredit(int loanId, [FromBody] LoanModel updatedLoan)
        {
            return await _credit.UpdateOneCredit(loanId, updatedLoan);
        }


        [HttpGet("user/allcredits")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LoanModel>>> GetAllLoans()
        {
            var userId = User.FindFirstValue("UserId");
            return await _credit.GetAllLoansForUser(userId);
        }

        [HttpGet("user/allcreditsBetween")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LoanModel>>> GetAllLoansBetween(DateTime startDate, DateTime endDate)
        {
            var userId = User.FindFirstValue("UserId");
            return await _credit.GetAllLoansForUserBetween(userId,startDate,endDate);
        }
    }


}