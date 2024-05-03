using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class File
    {
        public int FileId { get; set; }
        public string FileType { get; set; } = null!;
        public byte[] FileInfo { get; set; } = null!;
        public int LoanLoanId { get; set; }
        public int InsuranceInsuranceId { get; set; }
        public int TransactionTransactionId { get; set; }

        public virtual Insurance InsuranceInsurance { get; set; } = null!;
        public virtual Loan LoanLoan { get; set; } = null!;
        public virtual Transaction TransactionTransaction { get; set; } = null!;
    }
}
