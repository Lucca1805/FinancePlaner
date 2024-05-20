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
namespace FinanceLiquidityManager.Infrastructure.Insurance
{
    
    public class InsuranceHandler: ControllerBase
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

        public async Task<ActionResult<InsuranceModel>> GetOneInsurance(int insuranceId)
        {
            try
            {
                string query = @"SELECT * FROM finance.insurance WHERE InsuranceId = @InsuranceId";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var insurance = await connection.QueryFirstOrDefaultAsync<InsuranceModel>(query, new { InsuranceId = insuranceId });
                    if (insurance != null)
                    {
                        return Ok(insurance);
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

        
        public async Task<ActionResult<IEnumerable<InsuranceModel>>> GetAllInsuranceForUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Extrahiert die UserID aus dem JWT-Token

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                string query = @"SELECT * FROM finance.insurance WHERE PolicyHolderId = @PolicyHolderId";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var insurances = await connection.QueryAsync<InsuranceModel>(query, new { PolicyHolderId = userId });
                    return Ok(insurances);
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable To Process Request");
            }
        }

        public async Task<ActionResult> DeleteOneInsurance(int insuranceId)
        {
            try
            {
                string query = @"DELETE FROM finance.insurance WHERE InsuranceId = @InsuranceId";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var affectedRows = await connection.ExecuteAsync(query, new { InsuranceId = insuranceId });
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

        public async Task<ActionResult> UpdateOneInsurance(int insuranceId, [FromBody] InsuranceModel updatedInsurance)
        {
            if (insuranceId != updatedInsurance.InsuranceId)
            {
                return BadRequest("InsuranceId mismatch");
            }

            try
            {
                string query = @"
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

                using (var connection = new MySqlConnection(connectionString))
                {
                    var affectedRows = await connection.ExecuteAsync(query, updatedInsurance);
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