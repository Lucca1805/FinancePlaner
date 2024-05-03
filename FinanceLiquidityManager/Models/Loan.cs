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
            LoanPayments = new HashSet<LoanPayment>();
            PersonPeople = new HashSet<Person>();
        }

        public int LoanId { get; set; }
        public string LoanType { get; set; } = null!;
        public decimal LoanAmount { get; set; }
        public string LoanUnit { get; set; } = null!;
        public decimal InterestRate { get; set; }
        public string InterestRateUnit { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string LoanStatus { get; set; } = null!;
        /// <summary>
        /// Zahlungsinterval
        /// </summary>
        public string PaymentInterval { get; set; } = null!;

        public virtual ICollection<File> Files { get; set; }
        public virtual ICollection<LoanPayment> LoanPayments { get; set; }

        public virtual ICollection<Person> PersonPeople { get; set; }
    }
}
