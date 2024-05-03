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
            Insurances = new HashSet<Insurance>();
            SavingPlans = new HashSet<SavingPlan>();
            Transactions = new HashSet<Transaction>();
            LoanLoans = new HashSet<Loan>();
        }

        public int PersonId { get; set; }
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Password { get; set; }

        public virtual ICollection<Account> Accounts { get; set; }
        public virtual ICollection<Insurance> Insurances { get; set; }
        public virtual ICollection<SavingPlan> SavingPlans { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }

        public virtual ICollection<Loan> LoanLoans { get; set; }
    }
}
