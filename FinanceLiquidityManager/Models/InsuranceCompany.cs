using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class InsuranceCompany
    {
        public InsuranceCompany()
        {
            Insurances = new HashSet<Insurance>();
            SavingPlans = new HashSet<SavingPlan>();
        }

        public int InsuranceCompanyId { get; set; }
        public string InsuranceCompany1 { get; set; } = null!;
        public string? Description { get; set; }
        public string Country { get; set; } = null!;

        public virtual ICollection<Insurance> Insurances { get; set; }
        public virtual ICollection<SavingPlan> SavingPlans { get; set; }
    }
}
