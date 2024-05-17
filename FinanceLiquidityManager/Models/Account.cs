using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class Account
    {
        public Account()
        {
            Insurances = new HashSet<Insurance>();
            Loans = new HashSet<Loan>();
            StandingOrders = new HashSet<StandingOrder>();
            Transactions = new HashSet<Transaction>();
        }

        public string AccountId { get; set; } = null!;
        public string? Currency { get; set; }
        public string? AccountType { get; set; }
        public string? AccountSubType { get; set; }
        public string? Nickname { get; set; }
        public string? SchemeName { get; set; }
        public string Identification { get; set; } = null!;
        public int Name { get; set; }
        public string? SecondaryIdentification { get; set; }

        public virtual Person NameNavigation { get; set; } = null!;
        public virtual ICollection<Insurance> Insurances { get; set; }
        public virtual ICollection<Loan> Loans { get; set; }
        public virtual ICollection<StandingOrder> StandingOrders { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
