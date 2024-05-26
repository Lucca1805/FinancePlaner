-- Created by Vertabelo (http://vertabelo.com)
-- Last modification date: 2024-03-29 13:18:36.888

-- tables
-- Table: account
use finance;

CREATE TABLE finance.person (
    PersonId int AUTO_INCREMENT PRIMARY KEY,
    Email varchar(100)  NOT NULL,
    UserName varchar(70)  NOT NULL,
    Password varchar(60)  NOT NULL
) COMMENT 'Keeps information about each person that interacts with the bank';

CREATE TABLE finance.accounts (
    AccountId VARCHAR(40) PRIMARY KEY,
    Currency CHAR(3),
    AccountType VARCHAR(15),
    AccountSubType VARCHAR(20),
    Nickname VARCHAR(70),
    SchemeName VARCHAR(50),
    Identification VARCHAR(256) NOT NULL, -- account name that the account servicing institution assigns
    Name INT NOT NULL, 
    SecondaryIdentification VARCHAR(34),
	CONSTRAINT CHK_Currency CHECK (Currency IN ('USD', 'EUR', 'GBP')),
	CONSTRAINT CHK_AccountType CHECK (AccountType IN ('Business', 'Personal')),
	CONSTRAINT CHK_AccountSubType CHECK (AccountSubType IN ('CurrentAccount', 'Loan', 'Savings')),
	CONSTRAINT CHK_SchemeName CHECK (SchemeName IN ('UK.OBIE.IBAN', 'UK.OBIE.SortCodeAccountNumber', 'US.RoutingNumberAccountNumber', 'US.BranchCodeAccountNumber', 'UK.Revolut.InternalAccountId')),
    FOREIGN KEY (Name) REFERENCES person(PersonId)
);

CREATE TABLE finance.bank (
    BankId INT AUTO_INCREMENT PRIMARY KEY,
    DisplayName VARCHAR(50)  NOT NULL,
    Description VARCHAR(50)  NOT NULL,
    Country VARCHAR(50)  NOT NULL,
    BIC VARCHAR(11) UNIQUE NOT NULL,
    OrderNumber VARCHAR(70)  NOT NULL COMMENT 'Verfügernummer',
    OrderNumberPW INT  NOT NULL
);

CREATE TABLE finance.bank_account (
    BankId int AUTO_INCREMENT NOT NULL,
    AccountId VARCHAR(40) NOT NULL,
    PRIMARY KEY (BankId,AccountId)
); 

-- Table: insurance
CREATE TABLE finance.insurance (
    InsuranceId int AUTO_INCREMENT PRIMARY KEY,
    PolicyHolderId VARCHAR(40) NOT NULL,
    InsuranceType varchar(30)  NOT NULL,
    PaymentInstalmentAmount DECIMAL(13, 5) NOT NULL,
    PaymentInstalmentUnitCurrency VARCHAR(3),  
    DateOpened TIMESTAMP NOT NULL,
    DateClosed TIMESTAMP,
    InsuranceState boolean  NOT NULL,
    PaymentAmount DECIMAL(13, 5) NOT NULL,
    PaymentUnitCurrency VARCHAR(3),  
    Polizze longblob  NOT NULL,
    InsuranceCompany varchar(20)  NOT NULL,
    Description varchar(20),
    Country varchar(10)  NOT NULL,
    Frequency VARCHAR(35) NOT NULL COMMENT 'Zahlungsinterval',
    CONSTRAINT CHK_PaymentInstalmentUnitCurrency CHECK (PaymentInstalmentUnitCurrency IN ('USD', 'EUR', 'GBP')),
    CONSTRAINT CHK_PaymentUnitCurrency CHECK (PaymentUnitCurrency IN ('USD', 'EUR', 'GBP')),
    FOREIGN KEY (PolicyHolderId) REFERENCES accounts(AccountId)
);


-- Table: loan
CREATE TABLE finance.loan (
    LoanId int AUTO_INCREMENT PRIMARY KEY,
    CreditorAccountId VARCHAR(40) NOT NULL,
    LoanType varchar(20)  NOT NULL,
    LoanAmount DECIMAL(13, 5) NOT NULL,
    LoanUnitCurrency VARCHAR(3),  
    InterestRate DECIMAL(13, 5) NOT NULL,
    InterestRateUnitCurrency VARCHAR(3),  
    StartDate TIMESTAMP NOT NULL,
    EndDate TIMESTAMP,
    LoanStatus varchar(20)  NOT NULL,
    Frequency VARCHAR(35) NOT NULL COMMENT 'Zahlungsinterval',
    CONSTRAINT CHK_LoanUnitCurrency CHECK (LoanUnitCurrency IN ('USD', 'EUR', 'GBP')),
    CONSTRAINT CHK_InterestRateUnitCurrency CHECK (InterestRateUnitCurrency IN ('USD', 'EUR', 'GBP')),
    FOREIGN KEY (CreditorAccountId) REFERENCES accounts(AccountId)
) COMMENT 'Keeps information about the different loans that the bank grants to customers';

 -- Table: files
/*CREATE FUNCTION finance.CheckFileReference (FileType CHAR(1), RefID INT)
RETURNS BOOLEAN
BEGIN
    DECLARE isValid BOOLEAN DEFAULT FALSE;
    
    IF FileType = 'L' THEN
        SET isValid = EXISTS (SELECT 1 FROM finance.Loan WHERE LoanId = RefID);
    ELSEIF FileType = 'I' THEN
        SET isValid = EXISTS (SELECT 1 FROM finance.Insurance WHERE InsuranceId = RefID);
    END IF;
    
    RETURN isValid;
END */

CREATE TABLE finance.files (
    FileId int AUTO_INCREMENT PRIMARY KEY,
    FileInfo longblob  NOT NULL,
    FileType VARCHAR(1) NOT NULL,
    RefID INT NOT NULL,
    FOREIGN KEY (RefID) REFERENCES finance.loan(LoanId) on delete cascade,
    FOREIGN KEY (RefID) REFERENCES finance.insurance(InsuranceId) on delete cascade,
    CONSTRAINT CHK_FileType CHECK (FileType IN ('I', 'L'))  
);

CREATE TABLE finance.standingOrders (
    OrderId INT AUTO_INCREMENT PRIMARY KEY,
    CreditorAccountId VARCHAR(40) NOT NULL,
    Frequency VARCHAR(35) NOT NULL,
    NumberOfPayments INT,
    FirstPaymentDateTime TIMESTAMP NOT NULL,
    FinalPaymentDateTime TIMESTAMP,
    Reference VARCHAR(35) NOT NULL,
    FOREIGN KEY (CreditorAccountId) REFERENCES accounts(AccountId)
);

CREATE TABLE finance.transactions (
    TransactionId VARCHAR(40) PRIMARY KEY,
    AccountId VARCHAR(40) NOT NULL,
    CreditDebitIndicator VARCHAR(10),
    Status VARCHAR(20),
    BookingDateTime TIMESTAMP NOT NULL,
    ValueDateTime TIMESTAMP,
    Amount DECIMAL(13, 5) NOT NULL,
    AmountCurrency VARCHAR(3) NOT NULL,
    TransactionCode VARCHAR(35) NOT NULL,
    TransactionIssuer VARCHAR(35),
    TransactionInformation VARCHAR(500),
    MerchantName VARCHAR(350),
    ExchangeRate DECIMAL(20, 10),
    SourceCurrency VARCHAR(3),
    TargetCurrency VARCHAR(3),
    UnitCurrency VARCHAR(3),     
    InstructedAmount DECIMAL(13, 5),
    InstructedCurrency VARCHAR(3),
    BalanceCreditDebitIndicator VARCHAR(10),
    BalanceAmount DECIMAL(13, 5) NOT NULL,
    BalanceCurrency VARCHAR(3) NOT NULL,
    ChargeAmount DECIMAL(13, 5),
    ChargeCurrency VARCHAR(3) NOT NULL,
    SupplementaryData VARCHAR(40),
    CONSTRAINT CHK_CreditDebitIndicator CHECK (CreditDebitIndicator IN ('Credit', 'Debit')),
    CONSTRAINT CHK_Status CHECK (Status IN ('Booked', 'Pending')),
    CONSTRAINT CHK_AmountCurrency CHECK (AmountCurrency IN ('USD', 'EUR', 'GBP')),
    CONSTRAINT CHK_SourceCurrency CHECK (SourceCurrency IN ('USD', 'EUR', 'GBP')),
    CONSTRAINT CHK_TargetCurrency CHECK (TargetCurrency IN ('USD', 'EUR', 'GBP')),
    CONSTRAINT CHK_UnitCurrency CHECK (UnitCurrency IN ('USD', 'EUR', 'GBP')),
    CONSTRAINT CHK_InstructedCurrency CHECK (InstructedCurrency IN ('USD', 'EUR', 'GBP')),
    CONSTRAINT CHK_BalanceCreditDebitIndicator CHECK (BalanceCreditDebitIndicator IN ('Credit', 'Debit')),
    CONSTRAINT CHK_BalanceCurrency CHECK (BalanceCurrency IN ('USD', 'EUR', 'GBP')),
	CONSTRAINT CHK_ChargeCurrency CHECK (ChargeCurrency IN ('USD', 'EUR', 'GBP')),
    FOREIGN KEY (AccountId) REFERENCES accounts(AccountId)
);

INSERT INTO finance.person (Email, UserName, Password) VALUES
('person1@example.com', 'user1', '$2b$12$YsyA7BgyKgzHVvxlMHSRtOxpncpzchK1EFIMZGmrrwbR0zlXcAIHu'), -- password1
('person2@example.com', 'user2', '$2a$12$e8BJkWeGywFz0drfp5e40OYBJd2rQYrYcR5H4EX9hM5lVRfuJoWQu'), -- password2
('person3@example.com', 'user3', '$2a$12$u6hb9A1QY.3FQOafzE8qQOQ.YMdsI/PojpfxRGIpjKoU2fU5dLRr2'), -- password3
('person4@example.com', 'user4', '$2a$12$qE.yqWVgEOY1Shb04GdBvevN.Kedc1NftPZx6EB7nK7GiYvMz6pi2'), -- password4
('person5@example.com', 'user5', '$2a$12$ClmTUsqIn9UovPVST3H6BuoM8XrCxXnbV5F2zWnAfk8RxY5QUmuKO'), -- password5
('person6@example.com', 'user6', '$2a$12$M/fEgSHJERjI.RHDJ4Kye.KJGQ0soF4wsJStN10KnE63yQIqZflue'), -- password6
('person7@example.com', 'user7', '$2a$12$4.A0ZDi1TPlA6sF1YRXMpeHk26uPQhbP4Q5icZjApyyC3Te.1mY8G'), -- password7
('person8@example.com', 'user8', '$2a$12$EDqNqggz3J7.1HUzwQspXeNYN6dB80pYI9A5hO1.yG2fw.bmE1xDi'), -- password8
('person9@example.com', 'user9', '$2a$12$k/qL8MtI4J44ctY0MQUCTu0iJf9VgXTRIbmM60r8oiyPx8XtKROD2'), -- password9
('person10@example.com', 'user10', '$2a$12$R5T4N44cBh9NN6CmR5Ub6OC98Jfkgk1zK4hPiLRpVXfaVbCZlmybO'); -- password10

-- Insert Accounts for each Person
INSERT INTO finance.accounts (AccountId, Currency, AccountType, AccountSubType, Nickname, SchemeName, Identification, Name, SecondaryIdentification) VALUES
('ACC1001', 'USD', 'Personal', 'CurrentAccount', 'Person 1 Checking', 'US.RoutingNumberAccountNumber', '1234567890', 1, NULL),
('ACC1002', 'USD', 'Business', 'CurrentAccount', 'Person 1 Business', 'US.RoutingNumberAccountNumber', '0987654321', 1, NULL),
('ACC1003', 'EUR', 'Personal', 'Savings', 'Person 1 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201512345678', 1, NULL),
('ACC1004', 'GBP', 'Personal', 'Loan', 'Person 1 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201512345679', 1, NULL),
('ACC1005', 'USD', 'Personal', 'CurrentAccount', 'Person 2 Checking', 'US.RoutingNumberAccountNumber', '2345678901', 2, NULL),
('ACC1006', 'USD', 'Business', 'CurrentAccount', 'Person 2 Business', 'US.RoutingNumberAccountNumber', '8765432109', 2, NULL),
('ACC1007', 'EUR', 'Personal', 'Savings', 'Person 2 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201523456789', 2, NULL),
('ACC1008', 'GBP', 'Personal', 'Loan', 'Person 2 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201523456790', 2, NULL),
('ACC1009', 'USD', 'Personal', 'CurrentAccount', 'Person 3 Checking', 'US.RoutingNumberAccountNumber', '3456789012', 3, NULL),
('ACC1010', 'USD', 'Business', 'CurrentAccount', 'Person 3 Business', 'US.RoutingNumberAccountNumber', '7654321098', 3, NULL),
('ACC1011', 'EUR', 'Personal', 'Savings', 'Person 3 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201534567890', 3, NULL),
('ACC1012', 'GBP', 'Personal', 'Loan', 'Person 3 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201534567891', 3, NULL),
('ACC1013', 'USD', 'Personal', 'CurrentAccount', 'Person 4 Checking', 'US.RoutingNumberAccountNumber', '4567890123', 4, NULL),
('ACC1014', 'USD', 'Business', 'CurrentAccount', 'Person 4 Business', 'US.RoutingNumberAccountNumber', '6543210987', 4, NULL),
('ACC1015', 'EUR', 'Personal', 'Savings', 'Person 4 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201545678901', 4, NULL),
('ACC1016', 'GBP', 'Personal', 'Loan', 'Person 4 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201545678902', 4, NULL),
('ACC1017', 'USD', 'Personal', 'CurrentAccount', 'Person 5 Checking', 'US.RoutingNumberAccountNumber', '5678901234', 5, NULL),
('ACC1018', 'USD', 'Business', 'CurrentAccount', 'Person 5 Business', 'US.RoutingNumberAccountNumber', '5432109876', 5, NULL),
('ACC1019', 'EUR', 'Personal', 'Savings', 'Person 5 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201556789012', 5, NULL),
('ACC1020', 'GBP', 'Personal', 'Loan', 'Person 5 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201556789013', 5, NULL),
('ACC1021', 'USD', 'Personal', 'CurrentAccount', 'Person 6 Checking', 'US.RoutingNumberAccountNumber', '6789012345', 6, NULL),
('ACC1022', 'USD', 'Business', 'CurrentAccount', 'Person 6 Business', 'US.RoutingNumberAccountNumber', '4321098765', 6, NULL),
('ACC1023', 'EUR', 'Personal', 'Savings', 'Person 6 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201567890123', 6, NULL),
('ACC1024', 'GBP', 'Personal', 'Loan', 'Person 6 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201567890124', 6, NULL),
('ACC1025', 'USD', 'Personal', 'CurrentAccount', 'Person 7 Checking', 'US.RoutingNumberAccountNumber', '7890123456', 7, NULL),
('ACC1026', 'USD', 'Business', 'CurrentAccount', 'Person 7 Business', 'US.RoutingNumberAccountNumber', '3210987654', 7, NULL),
('ACC1027', 'EUR', 'Personal', 'Savings', 'Person 7 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201578901234', 7, NULL),
('ACC1028', 'GBP', 'Personal', 'Loan', 'Person 7 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201578901235', 7, NULL),
('ACC1029', 'USD', 'Personal', 'CurrentAccount', 'Person 8 Checking', 'US.RoutingNumberAccountNumber', '8901234567', 8, NULL),
('ACC1030', 'USD', 'Business', 'CurrentAccount', 'Person 8 Business', 'US.RoutingNumberAccountNumber', '2109876543', 8, NULL),
('ACC1031', 'EUR', 'Personal', 'Savings', 'Person 8 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201589012345', 8, NULL),
('ACC1032', 'GBP', 'Personal', 'Loan', 'Person 8 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201589012346', 8, NULL),
('ACC1033', 'USD', 'Personal', 'CurrentAccount', 'Person 9 Checking', 'US.RoutingNumberAccountNumber', '9012345678', 9, NULL),
('ACC1034', 'USD', 'Business', 'CurrentAccount', 'Person 9 Business', 'US.RoutingNumberAccountNumber', '1098765432', 9, NULL),
('ACC1035', 'EUR', 'Personal', 'Savings', 'Person 9 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201590123456', 9, NULL),
('ACC1036', 'GBP', 'Personal', 'Loan', 'Person 9 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201590123457', 9, NULL),
('ACC1037', 'USD', 'Personal', 'CurrentAccount', 'Person 10 Checking', 'US.RoutingNumberAccountNumber', '0123456789', 10, NULL),
('ACC1038', 'USD', 'Business', 'CurrentAccount', 'Person 10 Business', 'US.RoutingNumberAccountNumber', '0987654321', 10, NULL),
('ACC1039', 'EUR', 'Personal', 'Savings', 'Person 10 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201501234567', 10, NULL),
('ACC1040', 'GBP', 'Personal', 'Loan', 'Person 10 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201501234568', 10, NULL);

-- Insert 5 Banks for each Person
INSERT INTO finance.bank (DisplayName, Description, Country, BIC, OrderNumber, OrderNumberPW) VALUES
('Bank of America', 'Leading bank in the USA', 'United States', 'BOFAUS3N', '12345', 1234),
('Chase Bank', 'Major American bank', 'United States', 'CHASUS33', '54321', 4321),
('Wells Fargo', 'Major American bank', 'United States', 'WFBIUS6S', '67890', 5678),
('HSBC', 'British multinational bank', 'United Kingdom', 'MIDLGB22', '11122', 4321),
('Deutsche Bank', 'Major German bank', 'Germany', 'DEUTDEFF', '12134', 1221);

-- Insert Bank Account relations
INSERT INTO finance.bank_account (BankId, AccountId) VALUES
(1, 'ACC1001'),
(1, 'ACC1002'),
(2, 'ACC1003'),
(2, 'ACC1004'),
(3, 'ACC1005'),
(3, 'ACC1006'),
(4, 'ACC1007'),
(4, 'ACC1008'),
(5, 'ACC1009'),
(5, 'ACC1010'); 

-- Insert 3 Insurances per User
INSERT INTO finance.insurance (PolicyHolderId, InsuranceType, PaymentInstalmentAmount, PaymentInstalmentUnitCurrency, DateOpened, DateClosed, InsuranceState, PaymentAmount, PaymentUnitCurrency, Polizze, InsuranceCompany, Description, Country) VALUES
('ACC1001', 'Health', 150.50, 'USD', '2022-01-01 10:00:00', NULL, TRUE, 1500.00, 'USD', 'binary data', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1002', 'Car', 200.25, 'USD', '2022-06-01 10:00:00', NULL, TRUE, 2000.00, 'USD', 'binary data', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
('ACC1003', 'Life', 250.75, 'EUR', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 3000.00, 'EUR', 'binary data', 'AXA', 'Life Insurance', 'Germany', 'Monthly'),
('ACC1004', 'Health', 150.50, 'USD', '2022-01-01 10:00:00', NULL, TRUE, 1500.00, 'USD', 'binary data', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1005', 'Car', 200.25, 'USD', '2022-06-01 10:00:00', NULL, TRUE, 2000.00, 'USD', 'binary data', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
('ACC1006', 'Life', 250.75, 'EUR', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 3000.00, 'EUR', 'binary data', 'AXA', 'Life Insurance', 'Germany', 'Monthly'),
('ACC1007', 'Health', 150.50, 'USD', '2022-01-01 10:00:00', NULL, TRUE, 1500.00, 'USD', 'binary data', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1008', 'Car', 200.25, 'USD', '2022-06-01 10:00:00', NULL, TRUE, 2000.00, 'USD', 'binary data', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
('ACC1009', 'Life', 250.75, 'EUR', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 3000.00, 'EUR', 'binary data', 'AXA', 'Life Insurance', 'Germany', 'Monthly'),
('ACC1010', 'Health', 150.50, 'USD', '2022-01-01 10:00:00', NULL, TRUE, 1500.00, 'USD', 'binary data', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1011', 'Car', 200.25, 'USD', '2022-06-01 10:00:00', NULL, TRUE, 2000.00, 'USD', 'binary data', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
('ACC1012', 'Life', 250.75, 'EUR', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 3000.00, 'EUR', 'binary data', 'AXA', 'Life Insurance', 'Germany', 'Monthly');

-- Insert Loans per User (3 loans per user)
INSERT INTO finance.loan (CreditorAccountId, LoanType, LoanAmount, LoanUnitCurrency, InterestRate, InterestRateUnitCurrency, StartDate, EndDate, LoanStatus, Frequency) VALUES
('ACC1001', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly'),
('ACC1002', 'Car Loan', 20000.00, 'USD', 1.9, 'USD', '2021-10-01 09:00:00', '2025-10-01 09:00:00', 'Active', 'Monthly'),
('ACC1003', 'Student Loan', 30000.00, 'USD', 2.5, 'USD', '2021-07-01 09:00:00', '2028-07-01 09:00:00', 'Active', 'Monthly'),
('ACC1004', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly'),
('ACC1005', 'Car Loan', 20000.00, 'USD', 1.9, 'USD', '2021-10-01 09:00:00', '2025-10-01 09:00:00', 'Active', 'Monthly'),
('ACC1006', 'Student Loan', 30000.00, 'USD', 2.5, 'USD', '2021-07-01 09:00:00', '2028-07-01 09:00:00', 'Active', 'Monthly'),
('ACC1007', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly'),
('ACC1008', 'Car Loan', 20000.00, 'USD', 1.9, 'USD', '2021-10-01 09:00:00', '2025-10-01 09:00:00', 'Active', 'Monthly'),
('ACC1009', 'Student Loan', 30000.00, 'USD', 2.5, 'USD', '2021-07-01 09:00:00', '2028-07-01 09:00:00', 'Active', 'Monthly'),
('ACC1010', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly');

-- Insert 50 Standing Orders
INSERT INTO finance.standingOrders (CreditorAccountId, Frequency, NumberOfPayments, FirstPaymentDateTime, FinalPaymentDateTime, Reference) VALUES
('ACC1001', 'Monthly', 12, '2023-02-01 10:00:00', '2024-02-01 10:00:00', 'Subscription Payment'),
('ACC1002', 'Quarterly', 8, '2023-01-15 12:00:00', '2024-01-15 12:00:00', 'Service Payment'),
('ACC1003', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment'),
('ACC1004', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment'),
('ACC1005', 'Monthly', 6, '2023-05-01 10:00:00', '2023-10-01 10:00:00', 'Subscription Payment'),
('ACC1006', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment'),
('ACC1007', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment'),
('ACC1008', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment'),
('ACC1009', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment'),
('ACC1010', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment'),

('ACC1011', 'Monthly', 12, '2023-01-01 10:00:00', '2024-01-01 10:00:00', 'Subscription Payment'),
('ACC1012', 'Quarterly', 8, '2023-02-15 12:00:00', '2024-02-15 12:00:00', 'Service Payment'),
('ACC1013', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment'),
('ACC1014', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment'),
('ACC1015', 'Monthly', 6, '2023-05-01 10:00:00', '2023-10-01 10:00:00', 'Subscription Payment'),
('ACC1016', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment'),
('ACC1017', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment'),
('ACC1018', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment'),
('ACC1019', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment'),
('ACC1020', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment'),

('ACC1021', 'Monthly', 12, '2023-01-01 10:00:00', '2024-01-01 10:00:00', 'Subscription Payment'),
('ACC1022', 'Quarterly', 8, '2023-02-15 12:00:00', '2024-02-15 12:00:00', 'Service Payment'),
('ACC1023', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment'),
('ACC1024', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment'),
('ACC1025', 'Monthly', 6, '2023-05-01 10:00:00', '2023-10-01 10:00:00', 'Subscription Payment'),
('ACC1026', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment'),
('ACC1027', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment'),
('ACC1028', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment'),
('ACC1029', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment'),
('ACC1030', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment'),

('ACC1031', 'Monthly', 12, '2023-01-01 10:00:00', '2024-01-01 10:00:00', 'Subscription Payment'),
('ACC1032', 'Quarterly', 8, '2023-02-15 12:00:00', '2024-02-15 12:00:00', 'Service Payment'),
('ACC1033', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment'),
('ACC1034', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment'),
('ACC1035', 'Monthly', 6, '2023-05-01 10:00:00', '2023-10-01 10:00:00', 'Subscription Payment'),
('ACC1036', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment'),
('ACC1037', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment'),
('ACC1038', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment'),
('ACC1039', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment'),
('ACC1040', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2023-11-01 10:00:00', 'Gym Membership Payment');

-- Insert  Transactions
-- Insert into Transactions with TransactionId
INSERT INTO finance.transactions (
    TransactionId, AccountId, CreditDebitIndicator, Status, BookingDateTime, ValueDateTime, 
    Amount, AmountCurrency, TransactionCode, TransactionIssuer, 
    TransactionInformation, MerchantName, ExchangeRate, SourceCurrency, TargetCurrency, 
    UnitCurrency, InstructedAmount, InstructedCurrency, BalanceCreditDebitIndicator, 
    BalanceAmount, BalanceCurrency, ChargeAmount, ChargeCurrency, SupplementaryData
) VALUES
('TXN001', 'ACC1001', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 150.00, 'USD', 'T001', 'Issuer1', 'Salary Payment', 'Employer1', 1.0, 'USD', 'USD', 'USD', 150.00, 'USD', 'Credit', 5000.00, 'USD', 1.00, 'USD', 'Data1'),
('TXN002', 'ACC1001', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 80.00, 'USD', 'T002', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 1.0, 'USD', 'USD', 'USD', 80.00, 'USD', 'Debit', 4920.00, 'USD', 0.50, 'USD', 'Data2'),
('TXN003', 'ACC1001', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 25.00, 'USD', 'T003', 'Issuer3', 'Movie Ticket', 'Cinema1', 1.0, 'USD', 'USD', 'USD', 25.00, 'USD', 'Debit', 4895.00, 'USD', 0.25, 'USD', 'Data3'),
('TXN004', 'ACC1001', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 500.00, 'USD', 'T004', 'Issuer4', 'Bonus Payment', 'Employer2', 1.0, 'USD', 'USD', 'USD', 500.00, 'USD', 'Credit', 5395.00, 'USD', 1.00, 'USD', 'Data4'),
('TXN005', 'ACC1001', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 120.00, 'USD', 'T005', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 5275.00, 'USD', 0.75, 'USD', 'Data5'),

('TXN006', 'ACC1002', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 180.00, 'EUR', 'T006', 'Issuer1', 'Consulting Fee', 'Client1', 0.9, 'EUR', 'USD', 'EUR', 180.00, 'EUR', 'Credit', 3500.00, 'EUR', 2.00, 'EUR', 'Data6'),
('TXN007', 'ACC1002', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 90.00, 'EUR', 'T007', 'Issuer2', 'Electricity Bill', 'Utility1', 0.9, 'EUR', 'USD', 'EUR', 90.00, 'EUR', 'Debit', 3410.00, 'EUR', 1.00, 'EUR', 'Data7'),
('TXN008', 'ACC1002', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 50.00, 'EUR', 'T008', 'Issuer3', 'Internet Bill', 'ISP1', 0.9, 'EUR', 'USD', 'EUR', 50.00, 'EUR', 'Debit', 3360.00, 'EUR', 0.75, 'EUR', 'Data8'),
('TXN009', 'ACC1002', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 300.00, 'EUR', 'T009', 'Issuer4', 'Freelance Project', 'Client2', 0.9, 'EUR', 'USD', 'EUR', 300.00, 'EUR', 'Credit', 3660.00, 'EUR', 1.50, 'EUR', 'Data9'),
('TXN010', 'ACC1002', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 70.00, 'EUR', 'T010', 'Issuer5', 'Rent Payment', 'Landlord1', 0.9, 'EUR', 'USD', 'EUR', 70.00, 'EUR', 'Debit', 3590.00, 'EUR', 0.50, 'EUR', 'Data10'),

('TXN011', 'ACC1003', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 250.00, 'GBP', 'T011', 'Issuer1', 'Salary Payment', 'Employer1', 0.8, 'GBP', 'USD', 'GBP', 250.00, 'GBP', 'Credit', 4200.00, 'GBP', 2.50, 'GBP', 'Data11'),
('TXN012', 'ACC1003', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 110.00, 'GBP', 'T012', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.8, 'GBP', 'USD', 'GBP', 110.00, 'GBP', 'Debit', 4090.00, 'GBP', 1.25, 'GBP', 'Data12'),
('TXN013', 'ACC1003', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 35.00, 'GBP', 'T013', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.8, 'GBP', 'USD', 'GBP', 35.00, 'GBP', 'Debit', 4055.00, 'GBP', 0.75, 'GBP', 'Data13'),
('TXN014', 'ACC1003', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 450.00, 'GBP', 'T014', 'Issuer4', 'Bonus Payment', 'Employer2', 0.8, 'GBP', 'USD', 'GBP', 450.00, 'GBP', 'Credit', 4505.00, 'GBP', 3.00, 'GBP', 'Data14'),
('TXN015', 'ACC1003', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 130.00, 'GBP', 'T015', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.8, 'GBP', 'USD', 'GBP', 130.00, 'GBP', 'Debit', 4375.00, 'GBP', 1.50, 'GBP', 'Data15'),

('TXN016', 'ACC1004', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 200.00, 'USD', 'T016', 'Issuer1', 'Consulting Fee', 'Client1', 1.0, 'USD', 'USD', 'USD', 200.00, 'USD', 'Credit', 4500.00, 'USD', 1.50, 'USD', 'Data16'),
('TXN017', 'ACC1004', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 120.00, 'USD', 'T017', 'Issuer2', 'Electricity Bill', 'Utility1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 4380.00, 'USD', 0.50, 'USD', 'Data17'),
('TXN018', 'ACC1004', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 60.00, 'USD', 'T018', 'Issuer3', 'Internet Bill', 'ISP1', 1.0, 'USD', 'USD', 'USD', 60.00, 'USD', 'Debit', 4320.00, 'USD', 0.25, 'USD', 'Data18'),
('TXN019', 'ACC1004', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 350.00, 'USD', 'T019', 'Issuer4', 'Freelance Project', 'Client2', 1.0, 'USD', 'USD', 'USD', 350.00, 'USD', 'Credit', 4670.00, 'USD', 0.75, 'USD', 'Data19'),
('TXN020', 'ACC1004', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 90.00, 'USD', 'T020', 'Issuer5', 'Rent Payment', 'Landlord1', 1.0, 'USD', 'USD', 'USD', 90.00, 'USD', 'Debit', 4580.00, 'USD', 0.25, 'USD', 'Data20'),

('TXN021', 'ACC1005', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 220.00, 'GBP', 'T021', 'Issuer1', 'Salary Payment', 'Employer1', 0.85, 'GBP', 'USD', 'GBP', 220.00, 'GBP', 'Credit', 3600.00, 'GBP', 2.00, 'GBP', 'Data21'),
('TXN022', 'ACC1005', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 100.00, 'GBP', 'T022', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.85, 'GBP', 'USD', 'GBP', 100.00, 'GBP', 'Debit', 3500.00, 'GBP', 0.75, 'GBP', 'Data22'),
('TXN023', 'ACC1005', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 40.00, 'GBP', 'T023', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.85, 'GBP', 'USD', 'GBP', 40.00, 'GBP', 'Debit', 3460.00, 'GBP', 0.25, 'GBP', 'Data23'),
('TXN024', 'ACC1005', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 400.00, 'GBP', 'T024', 'Issuer4', 'Bonus Payment', 'Employer2', 0.85, 'GBP', 'USD', 'GBP', 400.00, 'GBP', 'Credit', 3860.00, 'GBP', 1.50, 'GBP', 'Data24'),
('TXN025', 'ACC1005', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 110.00, 'GBP', 'T025', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.85, 'GBP', 'USD', 'GBP', 110.00, 'GBP', 'Debit', 3750.00, 'GBP', 0.75, 'GBP', 'Data25'),

('TXN026', 'ACC1006', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 150.00, 'USD', 'T026', 'Issuer1', 'Salary Payment', 'Employer1', 1.0, 'USD', 'USD', 'USD', 150.00, 'USD', 'Credit', 4200.00, 'USD', 2.00, 'USD', 'Data26'),
('TXN027', 'ACC1006', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 80.00, 'USD', 'T027', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 1.0, 'USD', 'USD', 'USD', 80.00, 'USD', 'Debit', 4120.00, 'USD', 1.00, 'USD', 'Data27'),
('TXN028', 'ACC1006', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 25.00, 'USD', 'T028', 'Issuer3', 'Movie Ticket', 'Cinema1', 1.0, 'USD', 'USD', 'USD', 25.00, 'USD', 'Debit', 4095.00, 'USD', 0.25, 'USD', 'Data28'),
('TXN029', 'ACC1006', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 500.00, 'USD', 'T029', 'Issuer4', 'Bonus Payment', 'Employer2', 1.0, 'USD', 'USD', 'USD', 500.00, 'USD', 'Credit', 4595.00, 'USD', 1.00, 'USD', 'Data29'),
('TXN030', 'ACC1006', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 120.00, 'USD', 'T030', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 4475.00, 'USD', 0.75, 'USD', 'Data30'),

('TXN031', 'ACC1007', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 180.00, 'EUR', 'T031', 'Issuer1', 'Consulting Fee', 'Client1', 0.9, 'EUR', 'USD', 'EUR', 180.00, 'EUR', 'Credit', 3500.00, 'EUR', 2.00, 'EUR', 'Data31'),
('TXN032', 'ACC1007', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 90.00, 'EUR', 'T032', 'Issuer2', 'Electricity Bill', 'Utility1', 0.9, 'EUR', 'USD', 'EUR', 90.00, 'EUR', 'Debit', 3410.00, 'EUR', 1.00, 'EUR', 'Data32'),
('TXN033', 'ACC1007', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 50.00, 'EUR', 'T033', 'Issuer3', 'Internet Bill', 'ISP1', 0.9, 'EUR', 'USD', 'EUR', 50.00, 'EUR', 'Debit', 3360.00, 'EUR', 0.75, 'EUR', 'Data33'),
('TXN034', 'ACC1007', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 300.00, 'EUR', 'T034', 'Issuer4', 'Freelance Project', 'Client2', 0.9, 'EUR', 'USD', 'EUR', 300.00, 'EUR', 'Credit', 3660.00, 'EUR', 1.50, 'EUR', 'Data34'),
('TXN035', 'ACC1007', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 70.00, 'EUR', 'T035', 'Issuer5', 'Rent Payment', 'Landlord1', 0.9, 'EUR', 'USD', 'EUR', 70.00, 'EUR', 'Debit', 3590.00, 'EUR', 0.50, 'EUR', 'Data35'),

('TXN036', 'ACC1008', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 250.00, 'GBP', 'T036', 'Issuer1', 'Salary Payment', 'Employer1', 0.8, 'GBP', 'USD', 'GBP', 250.00, 'GBP', 'Credit', 4200.00, 'GBP', 2.50, 'GBP', 'Data36'),
('TXN037', 'ACC1008', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 110.00, 'GBP', 'T037', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.8, 'GBP', 'USD', 'GBP', 110.00, 'GBP', 'Debit', 4090.00, 'GBP', 1.25, 'GBP', 'Data37'),
('TXN038', 'ACC1008', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 35.00, 'GBP', 'T038', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.8, 'GBP', 'USD', 'GBP', 35.00, 'GBP', 'Debit', 4055.00, 'GBP', 0.75, 'GBP', 'Data38'),
('TXN039', 'ACC1008', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 450.00, 'GBP', 'T039', 'Issuer4', 'Bonus Payment', 'Employer2', 0.8, 'GBP', 'USD', 'GBP', 450.00, 'GBP', 'Credit', 4505.00, 'GBP', 3.00, 'GBP', 'Data39'),
('TXN040', 'ACC1008', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 130.00, 'GBP', 'T040', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.8, 'GBP', 'USD', 'GBP', 130.00, 'GBP', 'Debit', 4375.00, 'GBP', 1.50, 'GBP', 'Data40'),

('TXN041', 'ACC1009', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 200.00, 'USD', 'T041', 'Issuer1', 'Consulting Fee', 'Client1', 1.0, 'USD', 'USD', 'USD', 200.00, 'USD', 'Credit', 4500.00, 'USD', 1.50, 'USD', 'Data41'),
('TXN042', 'ACC1009', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 120.00, 'USD', 'T042', 'Issuer2', 'Electricity Bill', 'Utility1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 4380.00, 'USD', 0.50, 'USD', 'Data42'),
('TXN043', 'ACC1009', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 60.00, 'USD', 'T043', 'Issuer3', 'Internet Bill', 'ISP1', 1.0, 'USD', 'USD', 'USD', 60.00, 'USD', 'Debit', 4320.00, 'USD', 0.25, 'USD', 'Data43'),
('TXN044', 'ACC1009', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 350.00, 'USD', 'T044', 'Issuer4', 'Freelance Project', 'Client2', 1.0, 'USD', 'USD', 'USD', 350.00, 'USD', 'Credit', 4670.00, 'USD', 0.75, 'USD', 'Data44'),
('TXN045', 'ACC1009', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 90.00, 'USD', 'T045', 'Issuer5', 'Rent Payment', 'Landlord1', 1.0, 'USD', 'USD', 'USD', 90.00, 'USD', 'Debit', 4580.00, 'USD', 0.25, 'USD', 'Data45'),

('TXN046', 'ACC1010', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 220.00, 'GBP', 'T046', 'Issuer1', 'Salary Payment', 'Employer1', 0.85, 'GBP', 'USD', 'GBP', 220.00, 'GBP', 'Credit', 3600.00, 'GBP', 2.00, 'GBP', 'Data46'),
('TXN047', 'ACC1010', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 100.00, 'GBP', 'T047', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.85, 'GBP', 'USD', 'GBP', 100.00, 'GBP', 'Debit', 3500.00, 'GBP', 0.75, 'GBP', 'Data47'),
('TXN048', 'ACC1010', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 40.00, 'GBP', 'T048', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.85, 'GBP', 'USD', 'GBP', 40.00, 'GBP', 'Debit', 3460.00, 'GBP', 0.25, 'GBP', 'Data48'),
('TXN049', 'ACC1010', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 400.00, 'GBP', 'T049', 'Issuer4', 'Bonus Payment', 'Employer2', 0.85, 'GBP', 'USD', 'GBP', 400.00, 'GBP', 'Credit', 3860.00, 'GBP', 1.50, 'GBP', 'Data49'),
('TXN050', 'ACC1010', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 110.00, 'GBP', 'T050', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.85, 'GBP', 'USD', 'GBP', 110.00, 'GBP', 'Debit', 3750.00, 'GBP', 0.75, 'GBP', 'Data50'),

('TXN051', 'ACC1011', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 180.00, 'USD', 'T051', 'Issuer1', 'Consulting Fee', 'Client1', 1.0, 'USD', 'USD', 'USD', 180.00, 'USD', 'Credit', 4200.00, 'USD', 2.00, 'USD', 'Data51'),
('TXN052', 'ACC1011', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 90.00, 'USD', 'T052', 'Issuer2', 'Electricity Bill', 'Utility1', 1.0, 'USD', 'USD', 'USD', 90.00, 'USD', 'Debit', 4110.00, 'USD', 1.00, 'USD', 'Data52'),
('TXN053', 'ACC1011', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 50.00, 'USD', 'T053', 'Issuer3', 'Internet Bill', 'ISP1', 1.0, 'USD', 'USD', 'USD', 50.00, 'USD', 'Debit', 4060.00, 'USD', 0.75, 'USD', 'Data53'),
('TXN054', 'ACC1011', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 300.00, 'USD', 'T054', 'Issuer4', 'Freelance Project', 'Client2', 1.0, 'USD', 'USD', 'USD', 300.00, 'USD', 'Credit', 4360.00, 'USD', 1.50, 'USD', 'Data54'),
('TXN055', 'ACC1011', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 70.00, 'USD', 'T055', 'Issuer5', 'Rent Payment', 'Landlord1', 1.0, 'USD', 'USD', 'USD', 70.00, 'USD', 'Debit', 4290.00, 'USD', 0.50, 'USD', 'Data55'),

('TXN056', 'ACC1012', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 250.00, 'GBP', 'T056', 'Issuer1', 'Salary Payment', 'Employer1', 0.8, 'GBP', 'USD', 'GBP', 250.00, 'GBP', 'Credit', 3500.00, 'GBP', 2.00, 'GBP', 'Data56'),
('TXN057', 'ACC1012', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 120.00, 'GBP', 'T057', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.8, 'GBP', 'USD', 'GBP', 120.00, 'GBP', 'Debit', 3380.00, 'GBP', 1.25, 'GBP', 'Data57'),
('TXN058', 'ACC1012', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 60.00, 'GBP', 'T058', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.8, 'GBP', 'USD', 'GBP', 60.00, 'GBP', 'Debit', 3320.00, 'GBP', 0.75, 'GBP', 'Data58'),
('TXN059', 'ACC1012', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 400.00, 'GBP', 'T059', 'Issuer4', 'Bonus Payment', 'Employer2', 0.8, 'GBP', 'USD', 'GBP', 400.00, 'GBP', 'Credit', 3720.00, 'GBP', 2.50, 'GBP', 'Data59'),
('TXN060', 'ACC1012', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 110.00, 'GBP', 'T060', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.8, 'GBP', 'USD', 'GBP', 110.00, 'GBP', 'Debit', 3610.00, 'GBP', 0.75, 'GBP', 'Data60'),

('TXN061', 'ACC1013', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 150.00, 'USD', 'T061', 'Issuer1', 'Salary Payment', 'Employer1', 1.0, 'USD', 'USD', 'USD', 150.00, 'USD', 'Credit', 3500.00, 'USD', 2.00, 'USD', 'Data61'),
('TXN062', 'ACC1013', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 80.00, 'USD', 'T062', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 1.0, 'USD', 'USD', 'USD', 80.00, 'USD', 'Debit', 3420.00, 'USD', 1.00, 'USD', 'Data62');