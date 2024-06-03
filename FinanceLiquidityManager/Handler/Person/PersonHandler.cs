using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FinanceLiquidityManager.Handler.Login;
using FinanceLiquidityManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace FinanceLiquidityManager.Handler.Person
{
    public class PersonHandler
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public PersonHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            _connectionString = $"server={host};userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        public async Task<IActionResult> Update(UpdateUserRequest request)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var setClauses = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("@PersonId", request.PersonId);

                if (!string.IsNullOrEmpty(request.UserName))
                {
                    setClauses.Add("UserName = @UserName");
                    parameters.Add("@UserName", request.UserName);
                }

                if (!string.IsNullOrEmpty(request.Email))
                {
                    setClauses.Add("Email = @Email");
                    parameters.Add("@Email", request.Email);
                }

                if (!string.IsNullOrEmpty(request.CurrencyPreference))
                {
                    setClauses.Add("CurrencyPreference = @CurrencyPreference");
                    parameters.Add("@CurrencyPreference", request.CurrencyPreference);
                }

                if (setClauses.Count == 0)
                {
                    return new BadRequestObjectResult("No fields to update.");
                }

                var query = $"UPDATE person SET {string.Join(", ", setClauses)} WHERE PersonId = @PersonId";
                var result = await connection.ExecuteAsync(query, parameters);

                if (result > 0)
                {
                    return new OkObjectResult(new { Message = "User updated successfully." });
                }
                else
                {
                    return new BadRequestObjectResult("Failed to update user.");
                }
            }
        }

        public async Task<IActionResult> DummyData(string userId, string currencyPreference)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new BadRequestObjectResult("UserId not found.");
            }

            string query = @"
                SELECT CAST(SUBSTRING(AccountId FROM LENGTH(SUBSTRING_INDEX(AccountId, 'C', 1)) + 3) AS UNSIGNED) AS latestNumber 
                FROM finance.accounts 
                ORDER BY AccountId DESC 
                LIMIT 1";

            using (var connection = new MySqlConnection(_connectionString))
            {
                var latestAccountId = await connection.QuerySingleOrDefaultAsync<int?>(query);

                int newAccountNumber = latestAccountId.HasValue ? latestAccountId.Value + 1 : 1000;
                string newAccountId = $"ACC{newAccountNumber}";

                string insertAccountQuery = @"
                    INSERT INTO finance.accounts 
                    (AccountId, Currency, AccountType, AccountSubType, Nickname, SchemeName, Identification, Name, SecondaryIdentification)
                    VALUES 
                    (@AccountId, @Currency, @AccountType, @AccountSubType, @Nickname, @SchemeName, @Identification, @Name, @SecondaryIdentification)";

                var accountParameters1 = new
                {
                    AccountId = newAccountId,
                    Currency = currencyPreference,
                    AccountType = "Personal",
                    AccountSubType = "Loan",
                    Nickname = "Person 10 Loan",
                    SchemeName = "UK.OBIE.IBAN",
                    Identification = "GB12BARC20201501234568",
                    Name = userId,
                    SecondaryIdentification = (string)null
                };
                await connection.ExecuteAsync(insertAccountQuery, accountParameters1);

                string insertBankAccountQuery = @"INSERT INTO finance.bank_account (BankId, AccountId) VALUES (@BankId, @AccountId)";
                var bankAccountParameters1 = new
                {
                    BankId = 2,
                    AccountId = newAccountId
                };
                await connection.ExecuteAsync(insertBankAccountQuery, bankAccountParameters1);

                string insertInsuranceQuery = @"
                    INSERT INTO finance.insurance 
                    (PolicyHolderId, InsuranceType, PaymentInstalmentAmount, PaymentInstalmentUnitCurrency, DateOpened, DateClosed, InsuranceState, PaymentAmount, PaymentUnitCurrency, Polizze, InsuranceCompany, Description, Country, Frequency) 
                    VALUES
                    (@PolicyHolderId, @InsuranceType, @PaymentInstalmentAmount, @PaymentInstalmentUnitCurrency, @DateOpened, @DateClosed, @InsuranceState, @PaymentAmount, @PaymentUnitCurrency, @Polizze, @InsuranceCompany, @Description, @Country, @Frequency)";

                var insuranceParameters1 = new
                {
                    PolicyHolderId = newAccountId,
                    InsuranceType = "Health",
                    PaymentInstalmentAmount = 150.50,
                    PaymentInstalmentUnitCurrency = currencyPreference,
                    DateOpened = new DateTime(2022, 1, 1, 10, 0, 0),
                    DateClosed = (DateTime?)null,
                    InsuranceState = true,
                    PaymentAmount = 1500.00,
                    PaymentUnitCurrency = currencyPreference,
                    Polizze = "binary Data",
                    InsuranceCompany = "Allianz",
                    Description = "Health Insurance",
                    Country = "USA",
                    Frequency = "Monthly"
                };
                await connection.ExecuteAsync(insertInsuranceQuery, insuranceParameters1);

                string insertLoanQuery = @"
                INSERT INTO finance.loan 
                (CreditorAccountId, LoanType, LoanAmount, LoanUnitCurrency, InterestRate, InterestRateUnitCurrency, StartDate, EndDate, LoanStatus, Frequency, loanName, loanTerm, additionalCosts, effectiveInterestRate) 
                VALUES 
                (@CreditorAccountId, @LoanType, @LoanAmount, @LoanUnitCurrency, @InterestRate, @InterestRateUnitCurrency, @StartDate, @EndDate, @LoanStatus, @Frequency, @LoanName, @LoanTerm, @AdditionalCosts, @EffectiveInterestRate)";
                var loanParameters1 = new
                {
                    CreditorAccountId = newAccountId,
                    LoanType = "Mortgage",
                    LoanAmount = 250000.00,
                    LoanUnitCurrency = currencyPreference,
                    InterestRate = 3.5,
                    InterestRateUnitCurrency = currencyPreference,
                    StartDate = new DateTime(2020, 7, 1, 9, 0, 0),
                    EndDate = new DateTime(2030, 7, 1, 9, 0, 0),
                    LoanStatus = "Active",
                    Frequency = "Monthly",
                    LoanName = "Mortgage",
                    LoanTerm = 7300,
                    AdditionalCosts = 0.00,
                    EffectiveInterestRate = 3.5
                };
                await connection.ExecuteAsync(insertLoanQuery, loanParameters1);
                string standingOrderQuery = @"
                INSERT INTO finance.standingOrders 
                (CreditorAccountId, Frequency, NumberOfPayments, FirstPaymentDateTime, FinalPaymentDateTime, Reference, PaymentAmount, PaymentCurrency) 
                VALUES
                (@CreditorAccountId, @Frequency, @NumberOfPayments, @FirstPaymentDateTime, @FinalPaymentDateTime, @Reference, @PaymentAmount, @PaymentCurrency)";
                var standingOrderParameters1 = new
                {
                    CreditorAccountId = newAccountId,
                    Frequency = "Monthly",
                    NumberOfPayments = 12,
                    FirstPaymentDateTime = new DateTime(2024, 2, 1, 10, 0, 0),
                    FinalPaymentDateTime = new DateTime(2025, 2, 1, 10, 0, 0),
                    Reference = "Subscription Payment",
                    PaymentAmount = 15.00,
                    PaymentCurrency = currencyPreference
                };
                var standingOrderParameters2 = new
                {
                    CreditorAccountId = newAccountId,
                    Frequency = "Monthly",
                    NumberOfPayments = 24,
                    FirstPaymentDateTime = new DateTime(2024, 9, 1, 10, 0, 0),
                    FinalPaymentDateTime = new DateTime(2026, 9, 1, 10, 0, 0),
                    Reference = "Mortage Payment",
                    PaymentAmount = 1040.00,
                    PaymentCurrency = currencyPreference
                };
                await connection.ExecuteAsync(standingOrderQuery, standingOrderParameters1);
                await connection.ExecuteAsync(standingOrderQuery, standingOrderParameters2);

                // get the latest transaction Number
                string transNumberQuery = @"
                SELECT CAST(SUBSTRING(TransactionId FROM LENGTH(SUBSTRING_INDEX(TransactionId, 'N', 1)) + 2) AS UNSIGNED) AS latestNumber 
                FROM finance.transactions 
                ORDER BY TransactionId DESC 
                LIMIT 1";
                var latestTransactionId = await connection.QuerySingleOrDefaultAsync<int?>(query);

                int newTransactionNumber = latestTransactionId.HasValue ? latestTransactionId.Value + 1 : 10;

                string transactionQuery = @"
INSERT INTO finance.transactions (
    TransactionId, AccountId, CreditDebitIndicator, Status, BookingDateTime, ValueDateTime, 
    Amount, AmountCurrency, TransactionCode, TransactionIssuer, 
    TransactionInformation, MerchantName, ExchangeRate, SourceCurrency, TargetCurrency, 
    UnitCurrency, InstructedAmount, InstructedCurrency, BalanceCreditDebitIndicator, 
    BalanceAmount, BalanceCurrency, ChargeAmount, ChargeCurrency, SupplementaryData
) VALUES (
    @TransactionId, @AccountId, @CreditDebitIndicator, @Status, @BookingDateTime, @ValueDateTime, 
    @Amount, @AmountCurrency, @TransactionCode, @TransactionIssuer, 
    @TransactionInformation, @MerchantName, @ExchangeRate, @SourceCurrency, @TargetCurrency, 
    @UnitCurrency, @InstructedAmount, @InstructedCurrency, @BalanceCreditDebitIndicator, 
    @BalanceAmount, @BalanceCurrency, @ChargeAmount, @ChargeCurrency, @SupplementaryData
)";

                decimal previousBalance = 5000.00m; // Assuming initial balance
                for (int i = 0; i < 6; i++)
                {
                    int transactionAmount = 0;
                    string transactionInformation = "";
                    string merchantName = "";
                    string transactionIssuer = "";

                    switch (i)
                    {
                        case 0:
                            transactionAmount = 2000;
                            transactionInformation = "Salary Payment";
                            merchantName = "Employer1";
                            transactionIssuer = "Issuer1";
                            break;
                        case 1:
                            transactionAmount = 15;
                            transactionInformation = "Subscription Payment";
                            merchantName = "Netflix";
                            transactionIssuer = "Issuer2";
                            break;
                        case 2:
                            transactionAmount = 1040;
                            transactionInformation = "Mortage Payment";
                            merchantName = "Lender1";
                            transactionIssuer = "Issuer3";
                            break;
                        case 3:
                            transactionAmount = 25;
                            transactionInformation = "Movie Bill";
                            merchantName = "Cinema1";
                            transactionIssuer = "Issuer4";
                            break;
                        case 4:
                            transactionAmount = 500;
                            transactionInformation = "Bonus Payment";
                            merchantName = "Employer2";
                            transactionIssuer = "Issuer5";
                            break;
                        case 5:
                            transactionAmount = 120;
                            transactionInformation = "Restaurant Bill";
                            merchantName = "Restaurant1";
                            transactionIssuer = "Issuer6";
                            break;
                    }

                    string newTransactionId = $"TXN00{newTransactionNumber}";

                    var transactionParameters = new
                    {
                        TransactionId = newTransactionId,
                        AccountId = newAccountId,
                        CreditDebitIndicator = i % 2 == 0 ? "Credit" : "Debit",
                        Status = "Booked",
                        BookingDateTime = new DateTime(2024, 6, i + 1, 10, 0, 0),
                        ValueDateTime = new DateTime(2024, 6, i + 1, 10, 0, 0),
                        Amount = transactionAmount,
                        AmountCurrency = currencyPreference,
                        TransactionCode = $"T00{newTransactionNumber}",
                        TransactionIssuer = transactionIssuer,
                        TransactionInformation = transactionInformation,
                        MerchantName = merchantName,
                        ExchangeRate = 1.0,
                        SourceCurrency = currencyPreference,
                        TargetCurrency = currencyPreference,
                        UnitCurrency = currencyPreference,
                        InstructedAmount = transactionAmount,
                        InstructedCurrency = currencyPreference,
                        BalanceCreditDebitIndicator = i % 2 == 0 ? "Credit" : "Debit",
                        BalanceAmount = i % 2 == 0 ? previousBalance - transactionAmount : previousBalance + transactionAmount,
                        BalanceCurrency = currencyPreference,
                        ChargeAmount = i % 2 == 0 ? 1.00 : 0.5,
                        ChargeCurrency = currencyPreference,
                        SupplementaryData = $"Data{newTransactionNumber}"
                    };

                    await connection.ExecuteAsync(transactionQuery, transactionParameters);
                    previousBalance = transactionParameters.BalanceAmount; // Update previous balance
                    newTransactionNumber++;
                }



                newAccountNumber++;
                newAccountId = $"ACC{newAccountNumber}";
                var accountParameters2 = new
                {
                    AccountId = newAccountId,
                    Currency = currencyPreference,
                    AccountType = "Personal",
                    AccountSubType = "CurrentAccount",
                    Nickname = "Loan",
                    SchemeName = "US.RoutingNumberAccountNumber",
                    Identification = "0987654321",
                    Name = userId,
                    SecondaryIdentification = (string)null
                };
                await connection.ExecuteAsync(insertAccountQuery, accountParameters2);

                var bankAccountParameters2 = new
                {
                    BankId = 1,
                    AccountId = newAccountId
                };
                await connection.ExecuteAsync(insertBankAccountQuery, bankAccountParameters2);

                var insuranceParameters2 = new
                {
                    PolicyHolderId = newAccountId,
                    InsuranceType = "Car",
                    PaymentInstalmentAmount = 200.25,
                    PaymentInstalmentUnitCurrency = currencyPreference,
                    DateOpened = new DateTime(2022, 6, 1, 10, 0, 0),
                    DateClosed = (DateTime?)null,
                    InsuranceState = true,
                    PaymentAmount = 2000.00,
                    PaymentUnitCurrency = currencyPreference,
                    Polizze = "binary Data",
                    InsuranceCompany = "Progressive",
                    Description = "Car Insurance",
                    Country = "USA",
                    Frequency = "Monthly"
                };
                await connection.ExecuteAsync(insertInsuranceQuery, insuranceParameters2);
                var loanParameters2 = new
                {
                    CreditorAccountId = newAccountId,
                    LoanType = "Mortgage",
                    LoanAmount = 250000.00,
                    LoanUnitCurrency = currencyPreference,
                    InterestRate = 3.5,
                    InterestRateUnitCurrency = currencyPreference,
                    StartDate = new DateTime(2020, 7, 1, 9, 0, 0),
                    EndDate = new DateTime(2030, 7, 1, 9, 0, 0),
                    LoanStatus = "Active",
                    Frequency = "Monthly",
                    LoanName = "Mortgage",
                    LoanTerm = 7300,
                    AdditionalCosts = 0.00,
                    EffectiveInterestRate = 3.5
                };
                await connection.ExecuteAsync(insertLoanQuery, loanParameters2);
                var standingOrderParameters3 = new
                {
                    CreditorAccountId = newAccountId,
                    Frequency = "Monthly",
                    NumberOfPayments = 12,
                    FirstPaymentDateTime = new DateTime(2024, 2, 1, 10, 0, 0),
                    FinalPaymentDateTime = new DateTime(2025, 2, 1, 10, 0, 0),
                    Reference = "Insurance Payment",
                    PaymentAmount = 450.00,
                    PaymentCurrency = currencyPreference
                };
                var standingOrderParameters4 = new
                {
                    CreditorAccountId = newAccountId,
                    Frequency = "Annually",
                    NumberOfPayments = 5,
                    FirstPaymentDateTime = new DateTime(2024, 9, 1, 10, 0, 0),
                    FinalPaymentDateTime = new DateTime(2028, 9, 1, 10, 0, 0),
                    Reference = "Tax Payment",
                    PaymentAmount = 2500.00,
                    PaymentCurrency = currencyPreference
                };
                await connection.ExecuteAsync(standingOrderQuery, standingOrderParameters3);
                await connection.ExecuteAsync(standingOrderQuery, standingOrderParameters4);
              decimal previousBalance2 = 3500.00m; 
                for (int i = 0; i < 6; i++)
                {
                    int transactionAmount = 0;
                    string transactionInformation = "";
                    string merchantName = "";
                    string transactionIssuer = "";

                    switch (i)
                    {
                        case 0:
                            transactionAmount = 2000;
                            transactionInformation = "Salary Payment";
                            merchantName = "Employer1";
                            transactionIssuer = "Issuer1";
                            break;
                        case 1:
                            transactionAmount = 450;
                            transactionInformation = "Insurance Payment";
                            merchantName = "InsuranceCo1";
                            transactionIssuer = "Issuer2";
                            break;
                        case 2:
                            transactionAmount = 2000;
                            transactionInformation = "Tax Payment";
                            merchantName = "Tax Office 1";
                            transactionIssuer = "Issuer3";
                            break;
                        case 3:
                            transactionAmount = 25;
                            transactionInformation = "Movie Bill";
                            merchantName = "Cinema1";
                            transactionIssuer = "Issuer4";
                            break;
                        case 4:
                            transactionAmount = 500;
                            transactionInformation = "Bonus Payment";
                            merchantName = "Employer2";
                            transactionIssuer = "Issuer5";
                            break;
                        case 5:
                            transactionAmount = 120;
                            transactionInformation = "Restaurant Bill";
                            merchantName = "Restaurant1";
                            transactionIssuer = "Issuer6";
                            break;
                    }

                    string newTransactionId2 = $"TXN00{newTransactionNumber}";

                    var transactionParameters2 = new
                    {
                        TransactionId = newTransactionId2,
                        AccountId = newAccountId,
                        CreditDebitIndicator = i % 2 == 0 ? "Credit" : "Debit",
                        Status = "Booked",
                        BookingDateTime = new DateTime(2024, 6, i + 1, 10, 0, 0),
                        ValueDateTime = new DateTime(2024, 6, i + 1, 10, 0, 0),
                        Amount = transactionAmount,
                        AmountCurrency = currencyPreference,
                        TransactionCode = $"T00{newTransactionNumber}",
                        TransactionIssuer = transactionIssuer,
                        TransactionInformation = transactionInformation,
                        MerchantName = merchantName,
                        ExchangeRate = 1.0,
                        SourceCurrency = currencyPreference,
                        TargetCurrency = currencyPreference,
                        UnitCurrency = currencyPreference,
                        InstructedAmount = transactionAmount,
                        InstructedCurrency = currencyPreference,
                        BalanceCreditDebitIndicator = i % 2 == 0 ? "Credit" : "Debit",
                        BalanceAmount = i % 2 == 0 ? previousBalance2 - transactionAmount : previousBalance2 + transactionAmount,
                        BalanceCurrency = currencyPreference,
                        ChargeAmount = i % 2 == 0 ? 1.00 : 0.5,
                        ChargeCurrency = currencyPreference,
                        SupplementaryData = $"Data{newTransactionNumber}"
                    };

                    await connection.ExecuteAsync(transactionQuery, transactionParameters2);
                    previousBalance2 = transactionParameters2.BalanceAmount; // Update previous balance
                    newTransactionNumber++;
                }
            }

            return new OkObjectResult(new { Message = $"Dummy data populated successfully for User {userId}." });
        }
    }

    public class UpdateUserRequest
    {
        public int PersonId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string CurrencyPreference { get; set; }
    }
}