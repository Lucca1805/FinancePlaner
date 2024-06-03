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
using FinanceLiquidityManager.Handler.Transaction;

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

        [HttpPost("CreateDummyData")]
        public async Task<IActionResult> DummyData()
        {
            var userId = User.FindFirstValue("UserId");
            var CurrencyPreference = User.FindFirstValue("CurrencyPreference");
            return await _person.DummyData(userId,CurrencyPreference);
        }


    }
}
