
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
using FinanceLiquidityManager.Infrastructure.File;

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
        public async Task<ActionResult> UploadFileAsync(IFormFile file, string fileType, int refId)
        {
            return await _file.UploadFileAsync(file, fileType, refId);
        }

        [HttpGet("user/download/{CreditInsuranceID}")]
        public async Task<ActionResult> DownloadFileAsync(int creditId)
        {
            return await _file.DownloadFileAsync(creditId);
        }

    }


}