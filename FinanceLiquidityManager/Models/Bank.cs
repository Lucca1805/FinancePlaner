using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class Bank
    {
        public int BankId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string Bic { get; set; } = null!;
        /// <summary>
        /// VerfÃ¼gernummer
        /// </summary>
        public string OrderNumber { get; set; } = null!;
        public int OrderNumberPw { get; set; }
    }
}
