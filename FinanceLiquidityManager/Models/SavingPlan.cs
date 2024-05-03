using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class SavingPlan
    {
        public int SavingPlanId { get; set; }
        public string TargetGoal { get; set; } = null!;
        public decimal TargetAmount { get; set; }
        public string TargetAmountUnit { get; set; } = null!;
        public decimal CurrentAmount { get; set; }
        public string CurrentAmountUnit { get; set; } = null!;
        public DateTime OpenDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public bool State { get; set; }
        /// <summary>
        /// monatlich, jÃ¤hrlich, wÃ¶chentlich, quartalsweise
        /// </summary>
        public string PaymentInterval { get; set; } = null!;
        public int PersonPersonId { get; set; }
        public int BankBankId { get; set; }
        public int InsuranceCompanyInsuranceCompanyId { get; set; }
        public int TransactionTransactionId { get; set; }

        public virtual Bank BankBank { get; set; } = null!;
        public virtual InsuranceCompany InsuranceCompanyInsuranceCompany { get; set; } = null!;
        public virtual Person PersonPerson { get; set; } = null!;
        public virtual Transaction TransactionTransaction { get; set; } = null!;
    }
}
