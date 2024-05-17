using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class BankAccount
    {
        public int BankId { get; set; }
        public string AccountId { get; set; } = null!;
    }
}
