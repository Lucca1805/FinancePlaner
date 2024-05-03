using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    /// <summary>
    /// Keeps information about each scheduled Loan Payment
    /// </summary>
    public partial class LoanPayment
    {
        public LoanPayment()
        {
            Transactions = new HashSet<Transaction>();
        }

        public int LoanPaymentId { get; set; }
        public DateOnly ScheduledPaymentDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateOnly PaidDate { get; set; }
        public string PaymentType { get; set; } = null!;
        public int LoanLoanId { get; set; }

        public virtual Loan LoanLoan { get; set; } = null!;
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
