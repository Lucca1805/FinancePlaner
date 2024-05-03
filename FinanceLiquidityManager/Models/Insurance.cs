using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class Insurance
    {
        public Insurance()
        {
            Files = new HashSet<File>();
        }

        public int InsuranceId { get; set; }
        public string InsuranceType { get; set; } = null!;
        public decimal PaymentInstalment { get; set; }
        public string PaymentInstalmentUnit { get; set; } = null!;
        public DateTime DateOpened { get; set; }
        public bool InsuranceState { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime? DateClosed { get; set; }
        public string PaymentUnit { get; set; } = null!;
        public byte[] Polizze { get; set; } = null!;
        public int InsuranceCompanyInsuranceCompanyId { get; set; }
        public int PersonPersonId { get; set; }
        public int TransactionTransactionId { get; set; }

        public virtual InsuranceCompany InsuranceCompanyInsuranceCompany { get; set; } = null!;
        public virtual Person PersonPerson { get; set; } = null!;
        public virtual Transaction TransactionTransaction { get; set; } = null!;
        public virtual ICollection<File> Files { get; set; }
    }
}
