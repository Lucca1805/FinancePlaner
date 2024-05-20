
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
using FinanceLiquidityManager.Controllers;

namespace FinanceLiquidityManager.Infrastructure.Credit
{
   
    public class CreditHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<CreditController> _logger;
        public CreditHandler(IConfiguration configuration, ILogger<CreditController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            connectionString = $"server={host}; userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        public async Task<ActionResult<LoanModel>> GetOneCredit(int loanId)
        {
            try
            {
                string query = @"SELECT * FROM finance.loan WHERE LoanId = @LoanId";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var loan = await connection.QueryFirstOrDefaultAsync<LoanModel>(query, new { LoanId = loanId }, commandType: CommandType.Text);
                    if (loan != null)
                    {
                        return Ok(loan);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable To Process Request");
            }
        }

        public async Task<ActionResult> AddOneCredit([FromBody] LoanModel newLoan)
        {
            try
            {
                string query = @"
                    INSERT INTO finance.loan (
                        CreditorAccountId, 
                        LoanType, 
                        LoanAmount, 
                        LoanUnitCurrency, 
                        InterestRate, 
                        InterestRateUnitCurrency, 
                        StartDate, 
                        EndDate, 
                        LoanStatus, 
                        Frequency
                    ) VALUES (
                        @CreditorAccountId, 
                        @LoanType, 
                        @LoanAmount, 
                        @LoanUnitCurrency, 
                        @InterestRate, 
                        @InterestRateUnitCurrency, 
                        @StartDate, 
                        @EndDate, 
                        @LoanStatus, 
                        @Frequency
                    );
                    SELECT LAST_INSERT_ID();";

                using (var connection = new MySqlConnection(connectionString))
                {
                    var loanId = await connection.ExecuteScalarAsync<int>(query, newLoan);
                    return CreatedAtAction(nameof(GetOneCredit), new { loanId = loanId }, newLoan);
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable To Process Request");
            }
        }

        public async Task<ActionResult> DeleteOneCredit(int loanId)
        {
            try
            {
                string query = @"DELETE FROM finance.loan WHERE LoanId = @LoanId";

                using (var connection = new MySqlConnection(connectionString))
                {
                    var affectedRows = await connection.ExecuteAsync(query, new { LoanId = loanId });
                    if (affectedRows > 0)
                    {
                        return NoContent();
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable To Process Request");
            }
        }

        public async Task<ActionResult> UpdateOneCredit(int loanId, [FromBody] LoanModel updatedLoan)
        {
            if (loanId != updatedLoan.LoanId)
            {
                return BadRequest("LoanId mismatch");
            }

            try
            {
                string query = @"
                    UPDATE finance.loan
                    SET 
                        CreditorAccountId = @CreditorAccountId, 
                        LoanType = @LoanType, 
                        LoanAmount = @LoanAmount, 
                        LoanUnitCurrency = @LoanUnitCurrency, 
                        InterestRate = @InterestRate, 
                        InterestRateUnitCurrency = @InterestRateUnitCurrency, 
                        StartDate = @StartDate, 
                        EndDate = @EndDate, 
                        LoanStatus = @LoanStatus, 
                        Frequency = @Frequency
                    WHERE LoanId = @LoanId";

                using (var connection = new MySqlConnection(connectionString))
                {
                    var affectedRows = await connection.ExecuteAsync(query, updatedLoan);
                    if (affectedRows > 0)
                    {
                        return NoContent();
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable To Process Request");
            }
        }



    }

    public class LoanModel
    {
        public int LoanId { get; set; }
        public string CreditorAccountId { get; set; }
        public string LoanType { get; set; }
        public decimal LoanAmount { get; set; }
        public string LoanUnitCurrency { get; set; }
        public decimal InterestRate { get; set; }
        public string InterestRateUnitCurrency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string LoanStatus { get; set; }
        public string Frequency { get; set; }
    }
}