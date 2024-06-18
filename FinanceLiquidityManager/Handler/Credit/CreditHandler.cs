
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
using FinanceLiquidityManager.Handler.File;
using FinanceLiquidityManager.Handler.Insurance;

namespace FinanceLiquidityManager.Handler.Credit
{

    public class CreditHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<CreditController> _logger;
        private readonly FileHandler _file;
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



        public async Task<ActionResult> GetOneCredit(string userId, int loanId, string CurrencyPreference)
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
                    string loanQuery = @"SELECT l.* FROM finance.loan l WHERE l.LoanId = @LoanId;";
                    var loans = await connection.QueryAsync<LoanModel>(loanQuery, new { CreditorAccountId = accountIds, loanId });

                    foreach (LoanModel loan in loans)
                    {
                        if (loan.LoanUnitCurrency != CurrencyPreference)
                        {
                            decimal convertedCosts = await ConvertCurrency(loan.LoanAmount, loan.LoanUnitCurrency, CurrencyPreference);
                            loan.LoanAmount = Math.Round(convertedCosts, 2);
                            loan.LoanUnitCurrency = CurrencyPreference;
                            /*loan.LoanAmount = (decimal)CurrencyConverter.Convert(CurrencyPreference, loan.LoanUnitCurrency, (double)loan.LoanAmount);
                            loan.LoanUnitCurrency = CurrencyPreference;*/
                        }
                    }
                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(loans);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }



public async Task<ActionResult> AddLoan(string userId, [FromBody] LoanModelCreateRequest newLoan)
{
    try
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID is missing.");
        }

        // Fetch AccountId based on the user's name
        string queryAccountId = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var accountIds = await connection.QueryAsync<string>(queryAccountId, new { Name = userId });
            var accountId = accountIds.FirstOrDefault();

            // Check if accountId is null or empty
            if (string.IsNullOrEmpty(accountId))
            {
                return Unauthorized("User account not found.");
            }

            // Assign the accountId to the newLoan's CreditorAccountId
            newLoan.CreditorAccountId = accountId;

            // Initialize FileRequests property if it's null
            if (newLoan.FileRequests == null)
            {
                newLoan.FileRequests = new List<FileRequest>();
            }

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // Insert the loan record and retrieve the generated LoanId
                    string queryLoan = @"
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
                            Frequency,
                            loanName,
                            loanTerm,
                            additionalCosts,
                            effectiveInterestRate
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
                            @Frequency,
                            @loanName,
                            @loanTerm,
                            @additionalCosts,
                            @effectiveInterestRate
                        );
                        SELECT LAST_INSERT_ID();
                    ";

                    var parameters = new
                    {
                        CreditorAccountId = newLoan.CreditorAccountId,
                        LoanType = newLoan.LoanType,
                        LoanAmount = newLoan.LoanAmount,
                        LoanUnitCurrency = newLoan.LoanUnitCurrency,
                        InterestRate = newLoan.InterestRate,
                        InterestRateUnitCurrency = newLoan.InterestRateUnitCurrency,
                        StartDate = newLoan.StartDate,
                        EndDate = newLoan.EndDate,
                        LoanStatus = newLoan.LoanStatus,
                        Frequency = newLoan.Frequency,
                        loanName = newLoan.loanName,
                        loanTerm = newLoan.loanTerm,
                        additionalCosts = newLoan.additionalCosts,
                        effectiveInterestRate = newLoan.effectiveInterestRate
                    };

                    // Execute query to insert loan record and get loanId
                    var loanId = await connection.QuerySingleAsync<int>(queryLoan, parameters, transaction: transaction);

                    // Log the generated loanId
                    Console.WriteLine($"Generated LoanId: {loanId}");

                    // Commit the loan transaction first
                    await transaction.CommitAsync();

                    // Start a new transaction for file insertion
                    using (var fileTransaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Handle file uploads if FileRequests is provided and contains valid data
                            foreach (var fileRequest in newLoan.FileRequests)
                            {
                                // Check if the necessary fields are present in the FileRequest object
                                if (fileRequest == null || fileRequest.FileInfo == null || fileRequest.FileInfo.Length == 0 || string.IsNullOrEmpty(fileRequest.FileType))
                                {
                                    fileTransaction.Rollback();
                                    return BadRequest("Invalid file request data.");
                                }

                                // Get and sanitize the file name (if available in FileRequest)
                                string originalFileName = string.IsNullOrEmpty(fileRequest.FileName) ? "Unknown_File" : fileRequest.FileName;

                                Console.WriteLine($"Original file name: {originalFileName}");

                                // Insert the file data
                                var insertFileQuery = @"
                                    INSERT INTO finance.files (FileInfo, FileType, FileName, LoanID) 
                                    VALUES (@FileInfo, 'L', @FileName, @LoanID);
                                    SELECT LAST_INSERT_ID();
                                ";

                                var fileId = await connection.QuerySingleAsync<int>(insertFileQuery, new
                                {
                                    FileInfo = fileRequest.FileInfo,
                                    FileName = originalFileName,
                                    LoanID = loanId // Use the loanId for file association
                                }, transaction: fileTransaction);

                                if (fileId <= 0)
                                {
                                    fileTransaction.Rollback();
                                    return BadRequest("Failed to upload one or more files.");
                                }
                            }

                            // Commit the file transaction after successful file insertions
                            await fileTransaction.CommitAsync();

                            return Ok(new { LoanId = loanId, Message = "Loan and files added successfully." });
                        }
                        catch (Exception ex)
                        {
                            // Rollback the file transaction in case of any failure
                            fileTransaction.Rollback();
                            // Log the exception details (ex.Message) for debugging purposes.
                            Console.WriteLine($"Exception: {ex.Message}");
                            return StatusCode(500, $"Unable To Process Request: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Rollback the loan transaction in case of any failure
                    transaction.Rollback();
                    // Log the exception details (ex.Message) for debugging purposes.
                    Console.WriteLine($"Exception: {ex.Message}");
                    return StatusCode(500, $"Unable To Process Request: {ex.Message}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        // Log the exception details (ex.Message) for debugging purposes.
        Console.WriteLine($"Exception: {ex.Message}");
        return StatusCode(500, $"Unable To Process Request: {ex.Message}");
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
                            await connection.ExecuteAsync(deleteFilesQuery, new { LoanId = loanId }, transaction);

                            // Delete the credit record
                            string deleteCreditQuery = @"DELETE FROM finance.loan WHERE LoanId = @LoanId";
                            var affectedRows = await connection.ExecuteAsync(deleteCreditQuery, new { LoanId = loanId }, transaction);

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




        public async Task<ActionResult<IEnumerable<LoanModel>>> GetAllLoansForUser(string userId, string CurrencyPreference)
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
                    foreach (LoanModel loan in allLoans)
                    {
                        if (loan.LoanUnitCurrency != CurrencyPreference)
                        {
                            decimal convertedCosts = await ConvertCurrency(loan.LoanAmount, loan.LoanUnitCurrency, CurrencyPreference);
                            loan.LoanAmount = Math.Round(convertedCosts, 2);
                            loan.LoanUnitCurrency = CurrencyPreference;
                            /*_logger.LogInformation((int)loan.LoanAmount,CurrencyPreference,loan.LoanUnitCurrency);
                            loan.LoanAmount = (decimal)CurrencyConverter.Convert(CurrencyPreference, loan.LoanUnitCurrency, (double)loan.LoanAmount);
                            _logger.LogInformation((int)loan.LoanAmount,CurrencyPreference,loan.LoanUnitCurrency);*/
                            //loan.LoanUnitCurrency = CurrencyPreference;
                        }
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

        public async Task<ActionResult<IEnumerable<LoanModel>>> GetAllLoansForUserBetween(string userId, DateTime startDate, DateTime endDate, string CurrencyPreference)
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
                        var loans = await connection.QueryAsync<LoanModel>(loanQuery, new { CreditorAccountId = accountId, startDate = startDate, endDate = endDate });
                        allLoans.AddRange(loans);
                    }
                    foreach (LoanModel loan in allLoans)
                    {
                        if (loan.LoanUnitCurrency != CurrencyPreference)
                        {
                            /*loan.LoanAmount = (decimal)CurrencyConverter.Convert(CurrencyPreference, loan.LoanUnitCurrency, (double)loan.LoanAmount);
                            loan.LoanUnitCurrency = CurrencyPreference;*/
                            decimal convertedCosts = await ConvertCurrency(loan.LoanAmount, loan.LoanUnitCurrency, CurrencyPreference);
                            loan.LoanAmount = Math.Round(convertedCosts, 2);
                            loan.LoanUnitCurrency = CurrencyPreference;
                        }
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

        public async Task<ActionResult> UpdateOneCredit(string userId, int loanId, [FromBody] LoanPutModel updatedLoan)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(userId);
            }

            if (loanId != updatedLoan.LoanId)
            {
                return BadRequest("LoanId mismatch");
            }

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    // Fetch AccountIds based on the user's name
                    string queryAccounts = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                    var accountIds = await connection.QueryAsync<string>(queryAccounts, new { Name = userId });

                    if (accountIds == null || !accountIds.Any())
                    {
                        return NotFound("No accounts found for the given user.");
                    }

                    // Check if the CreditorAccountId of the loan matches one of the user's account IDs
                    if (!accountIds.Contains(updatedLoan.CreditorAccountId))
                    {
                        return Forbid("User is not authorized to update this loan.");
                    }

                    // Update the loan
                    string queryUpdate = @"
                UPDATE finance.loan
                SET 
                    CreditorAccountId = @CreditorAccountId, 
                    LoanAmount = @LoanAmount, 
                    InterestRate = @InterestRate, 
                    StartDate = @StartDate, 
                    EndDate = @EndDate,
                    LoanStatus = @LoanStatus,
                    Frequency = @Frequency,
                    loanName = @loanName,
                    loanTerm = @loanTerm,
                    additionalCosts = @additionalCosts
                WHERE LoanId = @LoanId";

                    var affectedRows = await connection.ExecuteAsync(queryUpdate, updatedLoan);
                    if (affectedRows > 0)
                    {
                        // Calculate the next payment and payment rate
                        var totalDays = (updatedLoan.EndDate.HasValue ?
                                         (updatedLoan.EndDate.Value - updatedLoan.StartDate).TotalDays :
                                         (DateTime.Now - updatedLoan.StartDate).TotalDays);
                        if (totalDays == 0) totalDays = 1; // To avoid division by zero

                        var frequencyFactor = 1;
                        switch (updatedLoan.Frequency.ToLower())
                        {
                            case "monthly":
                                frequencyFactor = 1;
                                break;
                            case "quarterly":
                                frequencyFactor = 3;
                                break;
                            case "yearly":
                                frequencyFactor = 12;
                                break;
                            default:
                                frequencyFactor = 1; // default to monthly
                                break;
                        }

                        var totalPeriods = (totalDays / 30) / frequencyFactor;
                        if (totalPeriods == 0) totalPeriods = 1; // To avoid division by zero

                        var paymentRate = Math.Round(updatedLoan.LoanAmount / (decimal)totalPeriods, 2);

                        // Calculate the next payment date based on frequency
                        DateTime nextPaymentDate;
                        switch (updatedLoan.Frequency.ToLower())
                        {
                            case "monthly":
                                nextPaymentDate = DateTime.Now.AddMonths(1);
                                break;
                            case "quarterly":
                                nextPaymentDate = DateTime.Now.AddMonths(3);
                                break;
                            case "yearly":
                                nextPaymentDate = DateTime.Now.AddYears(1);
                                break;
                            default:
                                nextPaymentDate = DateTime.Now.AddMonths(1); // default to monthly
                                break;
                        }

                        var result = new
                        {
                            updatedLoan.LoanId,
                            updatedLoan.CreditorAccountId,
                            updatedLoan.LoanAmount,
                            updatedLoan.InterestRate,
                            updatedLoan.StartDate,
                            updatedLoan.EndDate,
                            updatedLoan.LoanStatus,
                            updatedLoan.Frequency,
                            updatedLoan.loanName,
                            updatedLoan.loanTerm,
                            updatedLoan.additionalCosts,
                            nextPayment = nextPaymentDate,
                            paymentRate = (int)paymentRate
                        };

                        return Ok(result);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the loan.");
                return StatusCode(500, "Unable To Process Request");
            }
        }
        public async Task<decimal> ConvertCurrency(decimal amount, string baseCurrency, string targetCurrency)
        {
            try
            {
                // Use the CurrencyExchangeService to convert the amount
                CurrencyExchangeService exchangeService = new CurrencyExchangeService();
                return await exchangeService.ConvertCurrency(amount, baseCurrency, targetCurrency);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting currency: {ex.Message}");
            }
        }
    }
    public class LoanPutModel
    {
        public int LoanId { get; set; }
        public string CreditorAccountId { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Frequency { get; set; }
        public string? LoanStatus { get; set; }
        public string? InterestRateUnitCurrency { get; set; }
        public string? LoanUnitCurrency { get; set; }

        public string? loanName { get; set; }
        public int loanTerm { get; set; }
        public decimal? additionalCosts { get; set; }

    }
    public class LoanModel
    {
        public int LoanId { get; set; }
        public string CreditorAccountId { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Frequency { get; set; }
        public string? LoanStatus { get; set; }
        public string? InterestRateUnitCurrency { get; set; }
        public string? LoanUnitCurrency { get; set; }

        public string? loanName { get; set; }
        public int loanTerm { get; set; }
        public decimal? additionalCosts { get; set; }
        public string? FileInfo { get; set; }
        public string? FileType { get; set; }

    }

    public class LoanModelCreateRequest
    {
        public string CreditorAccountId { get; set; }
        public string LoanType { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Frequency { get; set; }
        public string? LoanStatus { get; set; }
        public string? InterestRateUnitCurrency { get; set; }
        public string? LoanUnitCurrency { get; set; }
        public string? loanName { get; set; }
        public int loanTerm { get; set; }
        public decimal? additionalCosts { get; set; }
        public decimal? effectiveInterestRate { get; set; }
        public List<FileRequest> FileRequests { get; set; } = new List<FileRequest>();


    }

    public class LoanUpdateRequest
    {
        public string Id { get; set; }
        public string IBAN { get; set; }
        public string StartingDate { get; set; }
        public float InterestRate { get; set; }
        public float LoanAmount { get; set; }
    }

    public class LoanResponse
    {
        public string Id { get; set; }
        public string IBAN { get; set; }
        public float AdditionalCosts { get; set; }
        public int Runtime { get; set; }
        public string StartingDate { get; set; }
        public float InterestRate { get; set; }
        public float EffectiveInterestRate { get; set; }
        public float LoanAmount { get; set; }
        public float TotalAmount { get; set; }
        public string PaymentRate { get; set; }
        public string NextPayment { get; set; }
        public List<string> Documents { get; set; }
    }

    public class AccountModel
    {
        public string AccountId { get; set; }
    }
}