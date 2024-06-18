
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
using System.IO.Compression;

namespace FinanceLiquidityManager.Handler.File
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

        public async Task<ActionResult> UploadFileAsync(int CreditInsuranceID, [FromBody] List<FileRequest> fileRequests)
        {
            try
            {
                // Check if the necessary fields are present in any of the FileRequest objects
                if (fileRequests == null || fileRequests.Count == 0 || fileRequests.Any(fr => fr == null || fr.FileInfo == null || fr.FileInfo.Length == 0 || string.IsNullOrEmpty(fr.FileType)))
                {
                    return BadRequest("Invalid file request data.");
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var fileResults = new List<object>();

                            foreach (var fileRequest in fileRequests)
                            {
                                // Get and sanitize the file name (if available in FileRequest)
                                string originalFileName = string.IsNullOrEmpty(fileRequest.FileName) ? "Unknown_File" : fileRequest.FileName;

                                Console.WriteLine($"Original file name: {originalFileName}");

                                // Insert the file data
                                var insertQuery = @"
                            INSERT INTO files (FileInfo, FileType, FileName, RefID) 
                            VALUES (@FileInfo, @FileType, @FileName, @RefID);
                            SELECT LAST_INSERT_ID();
                        ";

                                var fileId = await connection.QuerySingleAsync<int>(insertQuery, new
                                {
                                    FileInfo = fileRequest.FileInfo,
                                    FileType = fileRequest.FileType,
                                    FileName = originalFileName,
                                    RefID = CreditInsuranceID
                                }, transaction: transaction);

                                if (fileId > 0)
                                {
                                    fileResults.Add(new { FileId = fileId, FileName = originalFileName, Message = "File uploaded successfully." });
                                }
                                else
                                {
                                    // Rollback transaction if any file insertion fails
                                    transaction.Rollback();
                                    return BadRequest("Failed to upload one or more files.");
                                }
                            }

                            // Commit transaction after successful file insertions
                            transaction.Commit();

                            return Ok(new { Files = fileResults });
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction in case of any failure
                            transaction.Rollback();
                            // Log the exception details (ex.Message) for debugging purposes.
                            Console.WriteLine($"An error occurred: {ex.Message}");
                            return StatusCode(500, $"Internal Server Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }


        public async Task<ActionResult> DownloadFileAsync(int CreditInsuranceID)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Query to get the file data
                    var query = @"SELECT FileInfo, FileType FROM files WHERE FileId = @FileId AND (FileType = 'I' OR FileType = 'L')";
                    var file = await connection.QueryFirstOrDefaultAsync<FileRequest>(query, new { FileId = CreditInsuranceID });

                    if (file == null)
                    {
                        return NotFound("File not found.");
                    }

                    // Convert the file to a base64 string
                    var base64File = Convert.ToBase64String(file.FileInfo);

                    return Ok(new { FileType = file.FileType, Base64File = base64File });
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }


        public async Task<ActionResult> GetFilesByCreditInsuranceIDAsync(int CreditInsuranceID, string FileType)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Query to get all files for the given CreditInsuranceID
                    var query = @"SELECT FileId, FileName FROM files WHERE RefID = @RefID and FileType= @FileType";
                    var files = await connection.QueryAsync<FileResponse>(query, new { RefID = CreditInsuranceID, FileType = FileType });

                    if (files == null || !files.Any())
                    {
                        return NotFound("No files found for the given CreditInsuranceID.");
                    }

                    return Ok(files);
                }
            }
            catch (MySqlException sqlEx)
            {
                // Log the MySQL-specific error
                Console.WriteLine($"A database error occurred: {sqlEx.Message}");
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                // Log the general error
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }

        public async Task<ActionResult> RemoveFileAsync(int CreditInsuranceID, string FileName)
        {
            try
            {
                // Validate inputs
                if (CreditInsuranceID <= 0 || string.IsNullOrEmpty(FileName))
                {
                    return BadRequest("Invalid CreditInsuranceID or FileName.");
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the file exists and belongs to the specified CreditInsuranceID
                    string checkFileQuery = @"
                SELECT COUNT(*) FROM finance.files WHERE RefID = @CreditInsuranceID and FileName = @FileName;
            ";

                    // ExecuteScalarAsync will return the count directly
                    int count = await connection.ExecuteScalarAsync<int>(checkFileQuery, new { CreditInsuranceID, FileName });

                    if (count == 0)
                    {
                        return NotFound($"File '{FileName}' not found for the specified CreditInsuranceID.");
                    }

                    // Delete the file
                    string deleteFileQuery = @"DELETE FROM finance.files WHERE RefID = @CreditInsuranceID and FileName = @FileName";

                    // Use an anonymous object to pass parameters
                    await connection.ExecuteAsync(deleteFileQuery, new { CreditInsuranceID, FileName });

                    return Ok($"File '{FileName}' deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }


    }
}


public class FileRequest
{

    public byte[] FileInfo { get; set; } = null!;
    public string FileType { get; set; }
    public string FileName { get; set; }

}

public class FileResponse
{
    public long FileID { get; set; }
    public string FileName { get; set; }
}


