using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using FinanceLiquidityManager.Handler.Person;

namespace LoginController.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly PersonHandler _person;

        public PersonController(PersonHandler personHandler)
        {
            _person = personHandler;

        }

        [HttpPut("UpdatePerson")]
        public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)
        {
            return await _person.Update(request);
        }

        
    }
}
