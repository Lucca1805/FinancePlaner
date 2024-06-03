
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
using FinanceLiquidityManager.Handler.Credit;
using FinanceLiquidityManager.Handler.File;

namespace FinanceLiquidityManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreditController : ControllerBase
    {

        private readonly CreditHandler _credit;
        private readonly FileHandler _file;

        public CreditController(CreditHandler credit, FileHandler file)
        {
            _credit = credit;
            _file = file;

        }
        [HttpGet("user/credit/{loanId}")]
        [Authorize]
        public async Task<ActionResult<LoanModel>> GetOneCredit(int loanId)
        {
            var userId = User.FindFirstValue("UserId");
            return await _credit.GetOneCredit(userId, loanId);
        }

        [HttpPost("user/credit")]
        [Authorize]
        public async Task<ActionResult> AddOneCredit([FromBody] LoanModelCreateRequest newLoan)
        {
            var userId = User.FindFirstValue("UserId");
            return await _credit.AddLoan(userId, newLoan);
        }

        [HttpDelete("user/credit/{loanId}")]
        [Authorize]
        public async Task<ActionResult> DeleteOneCredit(int loanId)
        {
            var userId = User.FindFirstValue("UserId");
            return await _credit.DeleteOneCredit(userId, loanId);
        }

        [HttpPut("user/credit/{loanId}")]
        [Authorize]
        public async Task<ActionResult> UpdateOneCredit(int loanId, [FromBody] LoanPutModel updatedLoan)
        {
            var userId = User.FindFirstValue("UserId");
            return await _credit.UpdateOneCredit(userId,loanId, updatedLoan);
        }


        [HttpGet("user/allcredits")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LoanModel>>> GetAllLoans()
        {
            var userId = User.FindFirstValue("UserId");
            var loans = await _credit.GetAllLoansForUser(userId);

            /*var files;
            foreach(var loan in loans){
                var file = await _file.DownloadFileAsync(loan.LoandId);
                files.AddRange(file);
            }*/
            return loans;
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