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
        public virtual DbSet<File> Files { get; set; } = null!;
        public virtual DbSet<Insurance> Insurances { get; set; } = null!;
        public virtual DbSet<InsuranceCompany> InsuranceCompanies { get; set; } = null!;
        public virtual DbSet<Loan> Loans { get; set; } = null!;
        public virtual DbSet<LoanPayment> LoanPayments { get; set; } = null!;
        public virtual DbSet<Person> People { get; set; } = null!;
        public virtual DbSet<SavingPlan> SavingPlans { get; set; } = null!;
        public virtual DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseMySql("server=localhost;database=finance;user=root;password=Root0++", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.22-mysql"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_0900_ai_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("account");

                entity.HasComment("Keeps information about the different accounts each customer or group of customers can have in the bank");

                entity.HasIndex(e => new { e.AccountType, e.AccountNumber }, "account_ak_1")
                    .IsUnique();

                entity.HasIndex(e => e.BankBankId, "account_bank");

                entity.HasIndex(e => e.PersonPersonId, "person_account");

                entity.Property(e => e.AccountId)
                    .ValueGeneratedNever()
                    .HasColumnName("AccountID");

                entity.Property(e => e.AccountNumber).HasMaxLength(20);

                entity.Property(e => e.AccountType).HasMaxLength(20);

                entity.Property(e => e.BankBankId).HasColumnName("bank_bankID");

                entity.Property(e => e.CurrentBalance).HasPrecision(10, 2);

                entity.Property(e => e.PersonPersonId).HasColumnName("person_personID");

                entity.HasOne(d => d.BankBank)
                    .WithMany(p => p.Accounts)
                    .HasForeignKey(d => d.BankBankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("account_bank");

                entity.HasOne(d => d.PersonPerson)
                    .WithMany(p => p.Accounts)
                    .HasForeignKey(d => d.PersonPersonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("person_account");
            });

            modelBuilder.Entity<Bank>(entity =>
            {
                entity.ToTable("bank");

                entity.Property(e => e.BankId)
                    .ValueGeneratedNever()
                    .HasColumnName("bankID");

                entity.Property(e => e.Bic).HasColumnName("BIC");

                entity.Property(e => e.Country)
                    .HasMaxLength(50)
                    .HasColumnName("country");

                entity.Property(e => e.Description)
                    .HasMaxLength(50)
                    .HasColumnName("description");

                entity.Property(e => e.DisplayName)
                    .HasMaxLength(50)
                    .HasColumnName("displayName");

                entity.Property(e => e.OrderNumber)
                    .HasColumnName("orderNumber")
                    .HasComment("VerfÃ¼gernummer");

                entity.Property(e => e.OrderNumberPw).HasColumnName("orderNumberPW");
            });

            modelBuilder.Entity<File>(entity =>
            {
                entity.ToTable("files");

                entity.HasIndex(e => e.InsuranceInsuranceId, "files_insurance");

                entity.HasIndex(e => e.LoanLoanId, "files_loan");

                entity.HasIndex(e => e.TransactionTransactionId, "files_transaction");

                entity.Property(e => e.FileId)
                    .ValueGeneratedNever()
                    .HasColumnName("fileID");

                entity.Property(e => e.FileInfo).HasColumnName("fileInfo");

                entity.Property(e => e.FileType)
                    .HasMaxLength(20)
                    .HasColumnName("fileType");

                entity.Property(e => e.InsuranceInsuranceId).HasColumnName("insurance_insuranceID");

                entity.Property(e => e.LoanLoanId).HasColumnName("loan_loanID");

                entity.Property(e => e.TransactionTransactionId).HasColumnName("transaction_transactionID");

                entity.HasOne(d => d.InsuranceInsurance)
                    .WithMany(p => p.Files)
                    .HasForeignKey(d => d.InsuranceInsuranceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("files_insurance");

                entity.HasOne(d => d.LoanLoan)
                    .WithMany(p => p.Files)
                    .HasForeignKey(d => d.LoanLoanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("files_loan");

                entity.HasOne(d => d.TransactionTransaction)
                    .WithMany(p => p.Files)
                    .HasForeignKey(d => d.TransactionTransactionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("files_transaction");
            });

            modelBuilder.Entity<Insurance>(entity =>
            {
                entity.ToTable("insurance");

                entity.HasIndex(e => e.InsuranceCompanyInsuranceCompanyId, "insurance_insuranceCompany");

                entity.HasIndex(e => e.TransactionTransactionId, "insurance_transaction");

                entity.HasIndex(e => e.PersonPersonId, "person_insurance");

                entity.Property(e => e.InsuranceId)
                    .ValueGeneratedNever()
                    .HasColumnName("insuranceID");

                entity.Property(e => e.DateClosed)
                    .HasColumnType("datetime")
                    .HasColumnName("dateClosed");

                entity.Property(e => e.DateOpened)
                    .HasColumnType("datetime")
                    .HasColumnName("dateOpened");

                entity.Property(e => e.InsuranceCompanyInsuranceCompanyId).HasColumnName("insuranceCompany_insuranceCompanyID");

                entity.Property(e => e.InsuranceState).HasColumnName("insuranceState");

                entity.Property(e => e.InsuranceType)
                    .HasMaxLength(30)
                    .HasColumnName("insuranceType");

                entity.Property(e => e.PaymentAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("paymentAmount");

                entity.Property(e => e.PaymentInstalment)
                    .HasPrecision(10, 2)
                    .HasColumnName("paymentInstalment");

                entity.Property(e => e.PaymentInstalmentUnit)
                    .HasMaxLength(5)
                    .HasColumnName("paymentInstalmentUnit");

                entity.Property(e => e.PaymentUnit)
                    .HasMaxLength(5)
                    .HasColumnName("paymentUnit");

                entity.Property(e => e.PersonPersonId).HasColumnName("person_personID");

                entity.Property(e => e.Polizze).HasColumnName("polizze");

                entity.Property(e => e.TransactionTransactionId).HasColumnName("transaction_transactionID");

                entity.HasOne(d => d.InsuranceCompanyInsuranceCompany)
                    .WithMany(p => p.Insurances)
                    .HasForeignKey(d => d.InsuranceCompanyInsuranceCompanyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("insurance_insuranceCompany");

                entity.HasOne(d => d.PersonPerson)
                    .WithMany(p => p.Insurances)
                    .HasForeignKey(d => d.PersonPersonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("person_insurance");

                entity.HasOne(d => d.TransactionTransaction)
                    .WithMany(p => p.Insurances)
                    .HasForeignKey(d => d.TransactionTransactionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("insurance_transaction");
            });

            modelBuilder.Entity<InsuranceCompany>(entity =>
            {
                entity.ToTable("insuranceCompany");

                entity.Property(e => e.InsuranceCompanyId)
                    .ValueGeneratedNever()
                    .HasColumnName("insuranceCompanyID");

                entity.Property(e => e.Country)
                    .HasMaxLength(10)
                    .HasColumnName("country");

                entity.Property(e => e.Description)
                    .HasMaxLength(20)
                    .HasColumnName("description");

                entity.Property(e => e.InsuranceCompany1)
                    .HasMaxLength(20)
                    .HasColumnName("insuranceCompany");
            });

            modelBuilder.Entity<Loan>(entity =>
            {
                entity.ToTable("loan");

                entity.HasComment("Keeps information about the different loans that the bank grants to customers");

                entity.Property(e => e.LoanId)
                    .ValueGeneratedNever()
                    .HasColumnName("loanID");

                entity.Property(e => e.EndDate).HasColumnName("endDate");

                entity.Property(e => e.InterestRate)
                    .HasPrecision(10, 2)
                    .HasColumnName("interestRate");

                entity.Property(e => e.InterestRateUnit)
                    .HasMaxLength(5)
                    .HasColumnName("interestRateUnit");

                entity.Property(e => e.LoanAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("loanAmount");

                entity.Property(e => e.LoanStatus)
                    .HasMaxLength(20)
                    .HasColumnName("loanStatus");

                entity.Property(e => e.LoanType)
                    .HasMaxLength(20)
                    .HasColumnName("loanType");

                entity.Property(e => e.LoanUnit)
                    .HasMaxLength(5)
                    .HasColumnName("loanUnit");

                entity.Property(e => e.PaymentInterval)
                    .HasMaxLength(20)
                    .HasColumnName("paymentInterval")
                    .HasComment("Zahlungsinterval");

                entity.Property(e => e.StartDate).HasColumnName("startDate");
            });

            modelBuilder.Entity<LoanPayment>(entity =>
            {
                entity.ToTable("loanPayment");

                entity.HasComment("Keeps information about each scheduled Loan Payment");

                entity.HasIndex(e => e.LoanLoanId, "loanPayment_loan");

                entity.Property(e => e.LoanPaymentId)
                    .ValueGeneratedNever()
                    .HasColumnName("loanPaymentID");

                entity.Property(e => e.InterestAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("interestAmount");

                entity.Property(e => e.LoanLoanId).HasColumnName("loan_loanID");

                entity.Property(e => e.PaidAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("paidAmount");

                entity.Property(e => e.PaidDate).HasColumnName("paidDate");

                entity.Property(e => e.PaymentAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("paymentAmount");

                entity.Property(e => e.PaymentType)
                    .HasMaxLength(20)
                    .HasColumnName("paymentType");

                entity.Property(e => e.PrincipalAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("principalAmount");

                entity.Property(e => e.ScheduledPaymentDate).HasColumnName("scheduledPaymentDate");

                entity.HasOne(d => d.LoanLoan)
                    .WithMany(p => p.LoanPayments)
                    .HasForeignKey(d => d.LoanLoanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("loanPayment_loan");
            });

            modelBuilder.Entity<Person>(entity =>
            {
                entity.ToTable("person");

                entity.HasComment("Keeps information about each person that interacts with the bank");

                entity.Property(e => e.PersonId)
                    .ValueGeneratedNever()
                    .HasColumnName("personID");

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .HasColumnName("email");

                entity.Property(e => e.Password).HasColumnName("password");

                entity.Property(e => e.UserName)
                    .HasMaxLength(20)
                    .HasColumnName("userName");

                entity.HasMany(d => d.LoanLoans)
                    .WithMany(p => p.PersonPeople)
                    .UsingEntity<Dictionary<string, object>>(
                        "PersonLoan",
                        l => l.HasOne<Loan>().WithMany().HasForeignKey("LoanLoanId").OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("person_loan_loan"),
                        r => r.HasOne<Person>().WithMany().HasForeignKey("PersonPersonId").OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("person_loan_person"),
                        j =>
                        {
                            j.HasKey("PersonPersonId", "LoanLoanId").HasName("PRIMARY").HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                            j.ToTable("person_loan");

                            j.HasIndex(new[] { "LoanLoanId" }, "person_loan_loan");

                            j.IndexerProperty<int>("PersonPersonId").HasColumnName("person_personID");

                            j.IndexerProperty<int>("LoanLoanId").HasColumnName("loan_loanID");
                        });
            });

            modelBuilder.Entity<SavingPlan>(entity =>
            {
                entity.ToTable("savingPlan");

                entity.HasIndex(e => e.BankBankId, "savingPlan_bank");

                entity.HasIndex(e => e.InsuranceCompanyInsuranceCompanyId, "savingPlan_insuranceCompany");

                entity.HasIndex(e => e.PersonPersonId, "savingPlan_person");

                entity.HasIndex(e => e.TransactionTransactionId, "savingPlan_transaction");

                entity.Property(e => e.SavingPlanId)
                    .ValueGeneratedNever()
                    .HasColumnName("savingPlanID");

                entity.Property(e => e.BankBankId).HasColumnName("bank_bankID");

                entity.Property(e => e.ClosedDate)
                    .HasColumnType("datetime")
                    .HasColumnName("closedDate");

                entity.Property(e => e.CurrentAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("currentAmount");

                entity.Property(e => e.CurrentAmountUnit)
                    .HasMaxLength(5)
                    .HasColumnName("currentAmountUnit");

                entity.Property(e => e.InsuranceCompanyInsuranceCompanyId).HasColumnName("insuranceCompany_insuranceCompanyID");

                entity.Property(e => e.OpenDate)
                    .HasColumnType("datetime")
                    .HasColumnName("openDate");

                entity.Property(e => e.PaymentInterval)
                    .HasMaxLength(20)
                    .HasColumnName("paymentInterval")
                    .HasComment("monatlich, jÃ¤hrlich, wÃ¶chentlich, quartalsweise");

                entity.Property(e => e.PersonPersonId).HasColumnName("person_personID");

                entity.Property(e => e.State).HasColumnName("state");

                entity.Property(e => e.TargetAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("targetAmount");

                entity.Property(e => e.TargetAmountUnit)
                    .HasMaxLength(5)
                    .HasColumnName("targetAmountUnit");

                entity.Property(e => e.TargetGoal)
                    .HasMaxLength(20)
                    .HasColumnName("targetGoal");

                entity.Property(e => e.TransactionTransactionId).HasColumnName("transaction_transactionID");

                entity.HasOne(d => d.BankBank)
                    .WithMany(p => p.SavingPlans)
                    .HasForeignKey(d => d.BankBankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("savingPlan_bank");

                entity.HasOne(d => d.InsuranceCompanyInsuranceCompany)
                    .WithMany(p => p.SavingPlans)
                    .HasForeignKey(d => d.InsuranceCompanyInsuranceCompanyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("savingPlan_insuranceCompany");

                entity.HasOne(d => d.PersonPerson)
                    .WithMany(p => p.SavingPlans)
                    .HasForeignKey(d => d.PersonPersonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("savingPlan_person");

                entity.HasOne(d => d.TransactionTransaction)
                    .WithMany(p => p.SavingPlans)
                    .HasForeignKey(d => d.TransactionTransactionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("savingPlan_transaction");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transaction");

                entity.HasComment("Keeps information about every transaction performed on the Bank");

                entity.HasIndex(e => e.AccountAccountId, "transaction_account");

                entity.HasIndex(e => e.LoanPaymentLoanPaymentId, "transaction_loanPayment");

                entity.HasIndex(e => e.PersonPersonId, "transaction_person");

                entity.Property(e => e.TransactionId)
                    .ValueGeneratedNever()
                    .HasColumnName("transactionID");

                entity.Property(e => e.AccountAccountId).HasColumnName("account_AccountID");

                entity.Property(e => e.Amount)
                    .HasPrecision(10, 2)
                    .HasColumnName("amount");

                entity.Property(e => e.AmountUnit)
                    .HasMaxLength(5)
                    .HasColumnName("amountUnit");

                entity.Property(e => e.LoanPaymentLoanPaymentId).HasColumnName("loanPayment_loanPaymentID");

                entity.Property(e => e.PersonPersonId).HasColumnName("person_personID");

                entity.Property(e => e.TransactionDate)
                    .HasColumnType("datetime")
                    .HasColumnName("transactionDate");

                entity.Property(e => e.TransactionType)
                    .HasMaxLength(20)
                    .HasColumnName("transactionType");

                entity.HasOne(d => d.AccountAccount)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.AccountAccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("transaction_account");

                entity.HasOne(d => d.LoanPaymentLoanPayment)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.LoanPaymentLoanPaymentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("transaction_loanPayment");

                entity.HasOne(d => d.PersonPerson)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.PersonPersonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("transaction_person");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
