using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class File
    {
        public int FileId { get; set; }
        public byte[] FileInfo { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public int RefId { get; set; }

        public virtual Insurance Ref { get; set; } = null!;
        public virtual Loan RefNavigation { get; set; } = null!;
    }
}
