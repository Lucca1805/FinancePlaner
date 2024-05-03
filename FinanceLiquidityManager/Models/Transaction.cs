using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    /// <summary>
    /// Keeps information about every transaction performed on the Bank
    /// </summary>
    public partial class Transaction
    {
        public Transaction()
        {
            Files = new HashSet<File>();
            Insurances = new HashSet<Insurance>();
            SavingPlans = new HashSet<SavingPlan>();
        }

        public int TransactionId { get; set; }
        public string TransactionType { get; set; } = null!;
        public decimal Amount { get; set; }
        public string AmountUnit { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public int PersonPersonId { get; set; }
        public int AccountAccountId { get; set; }
        public int LoanPaymentLoanPaymentId { get; set; }

        public virtual Account AccountAccount { get; set; } = null!;
        public virtual LoanPayment LoanPaymentLoanPayment { get; set; } = null!;
        public virtual Person PersonPerson { get; set; } = null!;
        public virtual ICollection<File> Files { get; set; }
        public virtual ICollection<Insurance> Insurances { get; set; }
        public virtual ICollection<SavingPlan> SavingPlans { get; set; }
    }
}
