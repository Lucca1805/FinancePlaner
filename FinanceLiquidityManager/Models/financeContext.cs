using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FinanceLiquidityManager.Models
{
    public partial class financeContext : DbContext
    {
        public financeContext()
        {
        }

        public financeContext(DbContextOptions<financeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Accounts { get; set; } = null!;
        public virtual DbSet<Bank> Banks { get; set; } = null!;
        public virtual DbSet<BankAccount> BankAccounts { get; set; } = null!;
        public virtual DbSet<File> Files { get; set; } = null!;
        public virtual DbSet<Insurance> Insurances { get; set; } = null!;
        public virtual DbSet<Loan> Loans { get; set; } = null!;
        public virtual DbSet<Person> People { get; set; } = null!;
        public virtual DbSet<StandingOrder> StandingOrders { get; set; } = null!;
        public virtual DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseMySql("server=localhost;port=3306;database=finance;user=root;password=Root0++", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.22-mysql"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_0900_ai_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasIndex(e => e.Name, "Name");

                entity.Property(e => e.AccountId).HasMaxLength(40);

                entity.Property(e => e.AccountSubType).HasMaxLength(20);

                entity.Property(e => e.AccountType).HasMaxLength(15);

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .IsFixedLength();

                entity.Property(e => e.Identification).HasMaxLength(256);

                entity.Property(e => e.Nickname).HasMaxLength(70);

                entity.Property(e => e.SchemeName).HasMaxLength(50);

                entity.Property(e => e.SecondaryIdentification).HasMaxLength(34);

                entity.HasOne(d => d.NameNavigation)
                    .WithMany(p => p.Accounts)
                    .HasForeignKey(d => d.Name)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Accounts_ibfk_1");
            });

            modelBuilder.Entity<Bank>(entity =>
            {
                entity.ToTable("Bank");

                entity.HasIndex(e => e.Bic, "BIC")
                    .IsUnique();

                entity.Property(e => e.Bic)
                    .HasMaxLength(11)
                    .HasColumnName("BIC");

                entity.Property(e => e.Country).HasMaxLength(50);

                entity.Property(e => e.Description).HasMaxLength(50);

                entity.Property(e => e.DisplayName).HasMaxLength(50);

                entity.Property(e => e.OrderNumber)
                    .HasMaxLength(70)
                    .HasComment("VerfÃ¼gernummer");

                entity.Property(e => e.OrderNumberPw).HasColumnName("OrderNumberPW");
            });

            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.HasKey(e => new { e.BankId, e.AccountId })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity.ToTable("Bank_Account");

                entity.Property(e => e.BankId).ValueGeneratedOnAdd();

                entity.Property(e => e.AccountId).HasMaxLength(40);
            });

            modelBuilder.Entity<File>(entity =>
            {
                entity.HasIndex(e => e.RefId, "RefID");

                entity.Property(e => e.FileType).HasMaxLength(1);

                entity.Property(e => e.RefId).HasColumnName("RefID");

                entity.HasOne(d => d.Ref)
                    .WithMany(p => p.Files)
                    .HasForeignKey(d => d.RefId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Files_ibfk_2");

                entity.HasOne(d => d.RefNavigation)
                    .WithMany(p => p.Files)
                    .HasForeignKey(d => d.RefId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Files_ibfk_1");
            });

            modelBuilder.Entity<Insurance>(entity =>
            {
                entity.ToTable("Insurance");

                entity.HasIndex(e => e.PolicyHolderId, "PolicyHolderId");

                entity.Property(e => e.Country).HasMaxLength(10);

                entity.Property(e => e.DateClosed).HasColumnType("timestamp");

                entity.Property(e => e.DateOpened).HasColumnType("timestamp");

                entity.Property(e => e.Description).HasMaxLength(20);

                entity.Property(e => e.InsuranceCompany).HasMaxLength(20);

                entity.Property(e => e.InsuranceType).HasMaxLength(30);

                entity.Property(e => e.PaymentAmount).HasPrecision(13, 5);

                /*entity.Property(e => e.PaymentInstalmentAmount).HasPrecision(13, 5);

                entity.Property(e => e.PaymentInstalmentUnitCurrency).HasMaxLength(3);*/

                entity.Property(e => e.PaymentUnitCurrency).HasMaxLength(3);

                entity.Property(e => e.PolicyHolderId).HasMaxLength(40);

                entity.HasOne(d => d.PolicyHolder)
                    .WithMany(p => p.Insurances)
                    .HasForeignKey(d => d.PolicyHolderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Insurance_ibfk_1");
            });

            modelBuilder.Entity<Loan>(entity =>
            {
                entity.ToTable("Loan");

                entity.HasComment("Keeps information about the different loans that the bank grants to customers");

                entity.HasIndex(e => e.CreditorAccountId, "CreditorAccountId");

                entity.Property(e => e.CreditorAccountId).HasMaxLength(40);

                entity.Property(e => e.EndDate).HasColumnType("timestamp");

                entity.Property(e => e.Frequency)
                    .HasMaxLength(35)
                    .HasComment("Zahlungsinterval");

                entity.Property(e => e.InterestRate).HasPrecision(13, 5);

                entity.Property(e => e.InterestRateUnitCurrency).HasMaxLength(3);

                entity.Property(e => e.LoanAmount).HasPrecision(13, 5);

                entity.Property(e => e.LoanStatus).HasMaxLength(20);

                entity.Property(e => e.LoanType).HasMaxLength(20);

                entity.Property(e => e.LoanUnitCurrency).HasMaxLength(3);

                entity.Property(e => e.StartDate).HasColumnType("timestamp");

                entity.HasOne(d => d.CreditorAccount)
                    .WithMany(p => p.Loans)
                    .HasForeignKey(d => d.CreditorAccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Loan_ibfk_1");
            });

            modelBuilder.Entity<Person>(entity =>
            {
                entity.ToTable("Person");

                entity.HasComment("Keeps information about each person that interacts with the bank");

                entity.Property(e => e.Email).HasMaxLength(100);

                entity.Property(e => e.Password).HasMaxLength(60);

                entity.Property(e => e.UserName).HasMaxLength(70);
            });

            modelBuilder.Entity<StandingOrder>(entity =>
            {
                entity.HasKey(e => e.OrderId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.CreditorAccountId, "CreditorAccountId");

                entity.Property(e => e.CreditorAccountId).HasMaxLength(40);

                entity.Property(e => e.FinalPaymentDateTime).HasColumnType("timestamp");

                entity.Property(e => e.FirstPaymentDateTime).HasColumnType("timestamp");

                entity.Property(e => e.Frequency).HasMaxLength(35);

                entity.Property(e => e.Reference).HasMaxLength(35);

                entity.HasOne(d => d.CreditorAccount)
                    .WithMany(p => p.StandingOrders)
                    .HasForeignKey(d => d.CreditorAccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("StandingOrders_ibfk_1");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.AccountId, "AccountId");

                entity.Property(e => e.TransactionId).HasMaxLength(40);

                entity.Property(e => e.AccountId).HasMaxLength(40);

                entity.Property(e => e.Amount).HasPrecision(13, 5);

                entity.Property(e => e.AmountCurrency).HasMaxLength(3);

                entity.Property(e => e.BalanceAmount).HasPrecision(13, 5);

                entity.Property(e => e.BalanceCreditDebitIndicator).HasMaxLength(10);

                entity.Property(e => e.BalanceCurrency).HasMaxLength(3);

                entity.Property(e => e.BookingDateTime).HasColumnType("timestamp");

                entity.Property(e => e.ChargeAmount).HasPrecision(13, 5);

                entity.Property(e => e.ChargeCurrency).HasMaxLength(3);

                entity.Property(e => e.CreditDebitIndicator).HasMaxLength(10);

                entity.Property(e => e.ExchangeRate).HasPrecision(20, 10);

                entity.Property(e => e.InstructedAmount).HasPrecision(13, 5);

                entity.Property(e => e.InstructedCurrency).HasMaxLength(3);

                entity.Property(e => e.MerchantName).HasMaxLength(350);

                entity.Property(e => e.SourceCurrency).HasMaxLength(3);

                entity.Property(e => e.Status).HasMaxLength(20);

                entity.Property(e => e.SupplementaryData).HasMaxLength(40);

                entity.Property(e => e.TargetCurrency).HasMaxLength(3);

                entity.Property(e => e.TransactionCode).HasMaxLength(35);

                entity.Property(e => e.TransactionInformation).HasMaxLength(500);

                entity.Property(e => e.TransactionIssuer).HasMaxLength(35);

                entity.Property(e => e.UnitCurrency).HasMaxLength(3);

                entity.Property(e => e.ValueDateTime).HasColumnType("timestamp");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Transactions_ibfk_1");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
