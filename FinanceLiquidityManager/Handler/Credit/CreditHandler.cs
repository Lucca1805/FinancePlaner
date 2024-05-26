
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
using FinanceLiquidityManager.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using FinanceLiquidityManager.Handler.Insurance;

namespace FinanceLiquidityManager.Handler.Credit
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

        

        public async Task<ActionResult> GetOneCredit(string userId, int loanId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(userId);
                }

                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving AccountIds for user: {userId}", userId);
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found, return empty result
                    if (accountIds == null || !accountIds.Any())
                    {
                        _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                        return Ok(new List<LoanModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch insurances for each AccountId

                    _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountIds);
                    string loanQuery = @"SELECT * FROM finance.loan WHERE LoanId = @LoanId";
                    var insurance = await connection.QueryAsync<LoanModel>(loanQuery, new { CreditorAccountId = accountIds, loanId });


                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(insurance);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
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

        public async Task<ActionResult> DeleteOneCredit(string userId, int loanId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    if (accountIds == null || !accountIds.Any())
                    {
                        return Unauthorized();
                    }


                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Delete dependent records in the files table
                            string deleteFilesQuery = @"DELETE FROM finance.files WHERE RefID = @LoanId";
                            await connection.ExecuteAsync(deleteFilesQuery, new { LoandId = loanId }, transaction);

                            // Delete the credit record
                            string deleteCreditQuery = @"DELETE FROM finance.loan WHERE LoanId = @LoanId";
                            var affectedRows = await connection.ExecuteAsync(deleteCreditQuery, new { LoandId = loanId }, transaction);

                            // Commit the transaction if successful
                            await transaction.CommitAsync();

                            if (affectedRows > 0)
                            {
                                return NoContent();
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction in case of any errors
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "An error occurred while deleting the credit.");
                            return StatusCode(500, new
                            {
                                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                                title = "An error occurred while processing your request.",
                                status = 500,
                                traceId = HttpContext.TraceIdentifier
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable to process request");
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

        public async Task<ActionResult<IEnumerable<LoanModel>>> GetAllLoansForUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(userId);
                }

                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving AccountIds for user: {userId}", userId);
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found, return empty result
                    if (accountIds == null || !accountIds.Any())
                    {
                        _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                        return Ok(new List<LoanModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch loans for each AccountId
                    List<LoanModel> allLoans = new List<LoanModel>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching loans for AccountId: {accountId}", accountId);
                        string loanQuery = @"SELECT * FROM finance.loan WHERE CreditorAccountId = @CreditorAccountId";
                        var loans = await connection.QueryAsync<LoanModel>(loanQuery, new { CreditorAccountId = accountId });
                        allLoans.AddRange(loans);
                    }

                    _logger.LogInformation("All loans successfully retrieved.");
                    return Ok(allLoans);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }

        public async Task<ActionResult<IEnumerable<LoanModel>>> GetAllLoansForUserBetween(string userId,DateTime startDate, DateTime endDate)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(userId);
                }

                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving AccountIds for user: {userId}", userId);
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found, return empty result
                    if (accountIds == null || !accountIds.Any())
                    {
                        _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                        return Ok(new List<LoanModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch loans for each AccountId
                    List<LoanModel> allLoans = new List<LoanModel>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching loans for AccountId: {accountId}", accountId);
                        string loanQuery = @"SELECT * FROM finance.loan WHERE CreditorAccountId = @CreditorAccountId AND StartDate >= @startDate And EndDate <= @endDate";
                        var loans = await connection.QueryAsync<LoanModel>(loanQuery, new { CreditorAccountId = accountId,startDate = startDate,endDate= endDate });
                        allLoans.AddRange(loans);
                    }

                    _logger.LogInformation("All loans successfully retrieved.");
                    return Ok(allLoans);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
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

    public class AccountModel
    {
        public string AccountId { get; set; }
    }
}