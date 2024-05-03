using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    /// <summary>
    /// Keeps information about the different accounts each customer or group of customers can have in the bank
    /// </summary>
    public partial class Account
    {
        public Account()
        {
            Transactions = new HashSet<Transaction>();
        }

        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = null!;
        public string AccountType { get; set; } = null!;
        public decimal CurrentBalance { get; set; }
        public DateOnly DateOpened { get; set; }
        public DateOnly? DateClosed { get; set; }
        public bool AccountState { get; set; }
        public int PersonPersonId { get; set; }
        public int BankBankId { get; set; }

        public virtual Bank BankBank { get; set; } = null!;
        public virtual Person PersonPerson { get; set; } = null!;
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
