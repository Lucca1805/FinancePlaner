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
using System.Collections;

namespace FinanceLiquidityManager.Handler.Insurance
{

    public class InsuranceHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<InsuranceController> _logger;
        public InsuranceHandler(IConfiguration configuration, ILogger<InsuranceController> logger)
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



        public async Task<ActionResult> GetOneInsurance(string userId, int insuranceId)
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
                        return Ok(new List<InsuranceModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch insurances for each AccountId

                    _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountIds);
                    string insuranceQuery = @"SELECT * FROM finance.insurance WHERE InsuranceId = @InsuranceId";
                    var insurance = await connection.QueryAsync<InsuranceModel>(insuranceQuery, new { PolicyHolderId = accountIds, insuranceId });


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

        public async Task<ActionResult<IEnumerable<InsuranceModel>>> GetAllInsuranceForUser(string userId)
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
                        return Ok(new List<InsuranceModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch insurances for each AccountId
                    List<InsuranceModel> allStandingOrders = new List<InsuranceModel>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountId);
                        string insuranceQuery = @"SELECT * FROM finance.insurance WHERE PolicyHolderId = @PolicyHolderId";
                        var standingOrders = await connection.QueryAsync<InsuranceModel>(insuranceQuery, new { PolicyHolderId = accountId });
                        allStandingOrders.AddRange(standingOrders);
                    }

                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(allStandingOrders);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }


        public async Task<ActionResult> DeleteOneInsurance(string userId, int insuranceId)
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
                            string deleteFilesQuery = @"DELETE FROM finance.files WHERE RefID = @InsuranceId";
                            await connection.ExecuteAsync(deleteFilesQuery, new { InsuranceId = insuranceId }, transaction);

                            // Delete the credit record
                            string deleteCreditQuery = @"DELETE FROM finance.insurance WHERE InsuranceId = @InsuranceId";
                            var affectedRows = await connection.ExecuteAsync(deleteCreditQuery, new { InsuranceId = insuranceId }, transaction);

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
    

        public async Task<ActionResult> UpdateOneInsurance(string userId, int insuranceId, InsuranceModel updatedInsurance)
        {
            // Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user");
            }

            // Check if insuranceId in the request matches the one in the path
            if (insuranceId != updatedInsurance.InsuranceId)
            {
                return BadRequest("InsuranceId mismatch");
            }

            try
            {
                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found for the user, return Unauthorized
                    if (accountIds == null || !accountIds.Any())
                    {
                        return Unauthorized("User not authorized");
                    }

                    // Perform the update only if the user is authorized
                    string updateQuery = @"
                UPDATE finance.insurance
                SET 
                    PolicyHolderId = @PolicyHolderId, 
                    InsuranceType = @InsuranceType, 
                    PaymentInstalmentAmount = @PaymentInstalmentAmount, 
                    PaymentInstalmentUnitCurrency = @PaymentInstalmentUnitCurrency, 
                    DateOpened = @DateOpened, 
                    DateClosed = @DateClosed, 
                    InsuranceState = @InsuranceState, 
                    PaymentAmount = @PaymentAmount, 
                    PaymentUnitCurrency = @PaymentUnitCurrency, 
                    Polizze = @Polizze, 
                    InsuranceCompany = @InsuranceCompany, 
                    Description = @Description, 
                    Country = @Country
                WHERE InsuranceId = @InsuranceId";

                    var affectedRows = await connection.ExecuteAsync(updateQuery, updatedInsurance);
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
                return StatusCode(500, "Unable to process request");
            }
        }
    }

        public class InsuranceModel
    {
        public int InsuranceId { get; set; }
        public string PolicyHolderId { get; set; }
        public string InsuranceType { get; set; }
        public decimal PaymentInstalmentAmount { get; set; }
        public string PaymentInstalmentUnitCurrency { get; set; }
        public DateTime DateOpened { get; set; }
        public DateTime? DateClosed { get; set; }
        public bool InsuranceState { get; set; }
        public decimal PaymentAmount { get; set; }
        public string PaymentUnitCurrency { get; set; }
        public byte[] Polizze { get; set; }
        public string InsuranceCompany { get; set; }
        public string Description { get; set; }
        public string Country { get; set; }
    }

}