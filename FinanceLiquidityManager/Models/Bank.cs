using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class Bank
    {
        public Bank()
        {
            Accounts = new HashSet<Account>();
            SavingPlans = new HashSet<SavingPlan>();
        }

        public int BankId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Country { get; set; } = null!;
        public int Bic { get; set; }
        /// <summary>
        /// VerfÃ¼gernummer
        /// </summary>
        public long OrderNumber { get; set; }
        public int OrderNumberPw { get; set; }

        public virtual ICollection<Account> Accounts { get; set; }
        public virtual ICollection<SavingPlan> SavingPlans { get; set; }
    }
}
