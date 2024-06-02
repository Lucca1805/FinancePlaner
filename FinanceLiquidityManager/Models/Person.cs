using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    /// <summary>
    /// Keeps information about each person that interacts with the bank
    /// </summary>
    public partial class Person
    {
        public Person()
        {
            Accounts = new HashSet<Account>();
        }

        public int PersonId { get; set; }
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;

        public string CurrencyPreference {get; set;}

        public virtual ICollection<Account> Accounts { get; set; }
    }
}
