using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    /// <summary>
    /// Keeps information about the different loans that the bank grants to customers
    /// </summary>
    public partial class Loan
    {
        public Loan()
        {
            Files = new HashSet<File>();
        }

        public int LoanId { get; set; }
        public string CreditorAccountId { get; set; } = null!;
        public string LoanType { get; set; } = null!;
        public decimal LoanAmount { get; set; }
        public string? LoanUnitCurrency { get; set; }
        public decimal InterestRate { get; set; }
        public string? InterestRateUnitCurrency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string LoanStatus { get; set; } = null!;
        /// <summary>
        /// Zahlungsinterval
        /// </summary>
        public string Frequency { get; set; } = null!;

        public virtual Account CreditorAccount { get; set; } = null!;
        public virtual ICollection<File> Files { get; set; }
    }
}
