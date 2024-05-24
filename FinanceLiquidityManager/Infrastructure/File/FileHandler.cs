
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

namespace FinanceLiquidityManager.Infrastructure.File
{

    public class FileHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<CreditController> _logger;
        public FileHandler(IConfiguration configuration, ILogger<CreditController> logger)
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

        public async Task<ActionResult> UploadFileAsync(IFormFile fileData, string fileType, int refId)
        {
            try
            {
                // Check if the file exists and is valid
                if (fileData == null || fileData.Length == 0)
                {
                    return BadRequest("No file uploaded or file is empty.");
                }

                // Read the file data into a byte array
                byte[] fileBytes;
                using (var stream = new MemoryStream())
                {
                    await fileData.CopyToAsync(stream);
                    fileBytes = stream.ToArray();
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Insert the file data
                    var insertQuery = @"INSERT INTO files (FileInfo, FileType, RefID) VALUES (@FileInfo, @FileType, @RefID);
                                SELECT LAST_INSERT_ID();";
                    var fileId = await connection.QuerySingleAsync<int>(insertQuery, new
                    {
                        FileInfo = fileBytes,
                        FileType = fileType,
                        RefID = refId
                    });

                    if (fileId > 0)
                    {
                        return Ok(new { FileId = fileId, Message = "Datei erfolgreich hochgeladen." });
                    }

                    return BadRequest("Fehler beim Hochladen der Datei.");
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Ein Fehler ist aufgetreten: {ex.Message}");
                return StatusCode(500, "Interner Serverfehler");
            }
        }

        public async Task<ActionResult> DownloadFileAsync(int creditInsuranceID)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Query to get the file data
                    var query = @"SELECT FileInfo, FileType FROM files WHERE RefID = @RefID AND (FileType = 'I' OR FileType = 'L')";
                    var file = await connection.QuerySingleOrDefaultAsync<FileRequest>(query, new { RefID = creditInsuranceID });

                    if (file == null)
                    {
                        return NotFound("File not found.");
                    }

                    // Return the file as a download
                    return File(file.FileInfo, "application/octet-stream", $"file_{creditInsuranceID}.pdf");
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Ein Fehler ist aufgetreten: {ex.Message}");
                return StatusCode(500, "Interner Serverfehler");
            }
        }


    }

    public class FileRequest
    {

        public byte[] FileInfo { get; set; } = null!;
        public string FileType { get; set; }

    }

}
