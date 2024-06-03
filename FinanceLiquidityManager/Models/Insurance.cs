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
        public string PolicyHolderId { get; set; } = null!;
        public string InsuranceType { get; set; } = null!;
        public DateTime DateOpened { get; set; }
        public DateTime? DateClosed { get; set; }
        public bool InsuranceState { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? PaymentUnitCurrency { get; set; }
        public byte[] Polizze { get; set; } = null!;
        public string InsuranceCompany { get; set; } = null!;
        public string? Description { get; set; }
        public string Country { get; set; } = null!;

        public string Frequency { get; set; } = null!;

        public virtual Account PolicyHolder { get; set; } = null!;
        public virtual ICollection<File> Files { get; set; }
    }
}
