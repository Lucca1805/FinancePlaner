
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
using FinanceLiquidityManager.Handler.File;

namespace FinanceLiquidityManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {

        private readonly FileHandler _file;

        public FileController(FileHandler file)
        {
            _file = file;

        }


        [HttpPost("user/upload/{CreditInsuranceID}")]
        public async Task<ActionResult> UploadFileAsync(int CreditInsuranceID, [FromBody] List<FileRequest> fileRequest)
        {
            return await _file.UploadFileAsync(CreditInsuranceID, fileRequest);
        }


        [HttpGet("user/download/{CreditInsuranceID}")]
        public async Task<ActionResult> DownloadFilesAsync(int CreditInsuranceID)
        {
            return await _file.DownloadFilesAsync(CreditInsuranceID);
        }

        [HttpGet("user/files/{CreditInsuranceID}")]
        public async Task<ActionResult> GetFilesByCreditInsuranceIDAsync(int CreditInsuranceID)
        {
            return await _file.GetFilesByCreditInsuranceIDAsync(CreditInsuranceID);
        }

        [HttpDelete("user/delete/{CreditInsuranceID}")]
        public async Task<ActionResult> RemoveFileAsync(int CreditInsuranceID)
        {

            return await _file.RemoveFileAsync(CreditInsuranceID);
        }



    }


}