-- Created by Vertabelo (http://vertabelo.com)
-- Last modification date: 2024-03-29 13:18:36.888

-- tables
-- Table: account
use finance;

CREATE TABLE finance.person (
    PersonId int AUTO_INCREMENT PRIMARY KEY,
    Email varchar(100)  NOT NULL,
    UserName varchar(70)  NOT NULL,
    Password varchar(60)  NOT NULL,
    CurrencyPreference varchar(3) NOT NULL
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
	CONSTRAINT CHK_Currency CHECK (Currency IN ('USD', 'EUR', 'EUR')),
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
--    PaymentInstalmentAmount DECIMAL(13, 5) NOT NULL,
--    PaymentInstalmentUnitCurrency VARCHAR(3),  
    DateOpened TIMESTAMP NOT NULL,
    DateClosed TIMESTAMP,
    InsuranceState boolean  NOT NULL,
    PaymentAmount DECIMAL(13, 5) NOT NULL,
    PaymentUnitCurrency VARCHAR(3),  
--    Polizze longblob  NOT NULL,
    InsuranceCompany varchar(20)  NOT NULL,
    Description varchar(20),
    Country varchar(10)  NOT NULL,
    Frequency VARCHAR(35) NOT NULL COMMENT 'Zahlungsinterval',
    AdditionalInformation VARCHAR(100),
 --   CONSTRAINT CHK_PaymentInstalmentUnitCurrency CHECK (PaymentInstalmentUnitCurrency IN ('USD', 'EUR', 'EUR')),
    CONSTRAINT CHK_PaymentUnitCurrency CHECK (PaymentUnitCurrency IN ('USD', 'EUR', 'EUR')),
    FOREIGN KEY (PolicyHolderId) REFERENCES accounts(AccountId)
);


-- Table: loan loanTerm = Angabe der Dauer des loan ins Tagen
CREATE TABLE finance.loan (
    LoanId int AUTO_INCREMENT PRIMARY KEY,
    CreditorAccountId VARCHAR(40) NOT NULL,
    LoanType VARCHAR(20) NOT NULL,
    loanName VARCHAR(40),
    loanTerm int,
    additionalCosts DECIMAL(13,5),
    effectiveInterestRate DECIMAL(13,5),
    LoanAmount DECIMAL(13, 5) NOT NULL,
    LoanUnitCurrency VARCHAR(3),  
    InterestRate DECIMAL(13, 5) NOT NULL,
    InterestRateUnitCurrency VARCHAR(3),  
    StartDate TIMESTAMP NOT NULL,
    EndDate TIMESTAMP,
    LoanStatus VARCHAR(20) NOT NULL,
    Frequency VARCHAR(35) NOT NULL COMMENT 'Zahlungsinterval',
    CONSTRAINT CHK_LoanUnitCurrency CHECK (LoanUnitCurrency IN ('USD', 'EUR', 'EUR')),
    CONSTRAINT CHK_InterestRateUnitCurrency CHECK (InterestRateUnitCurrency IN ('USD', 'EUR', 'EUR')),
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
    FileInfo longblob NOT NULL,
    FileType VARCHAR(1) NOT NULL,
    FileName varchar(50) NOT NULL,
    LoanID INT,
    InsuranceID INT,
    CONSTRAINT CHK_FileType CHECK (FileType IN ('I', 'L')),
    FOREIGN KEY (LoanID) REFERENCES finance.loan(LoanId) ON DELETE CASCADE,
    FOREIGN KEY (InsuranceID) REFERENCES finance.insurance(InsuranceId) ON DELETE CASCADE
);


CREATE TABLE finance.standingOrders (
    OrderId INT AUTO_INCREMENT PRIMARY KEY,
    CreditorAccountId VARCHAR(40) NOT NULL,
    Frequency VARCHAR(35) NOT NULL,
    NumberOfPayments INT,
    FirstPaymentDateTime TIMESTAMP NOT NULL,
    FinalPaymentDateTime TIMESTAMP,
    Reference VARCHAR(35) NOT NULL,
    PaymentAmount DECIMAL(13,5) NOT NULL,
    PaymentCurrency VARCHAR(20) NOT NULL,
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
    CONSTRAINT CHK_AmountCurrency CHECK (AmountCurrency IN ('USD', 'EUR', 'EUR')),
    CONSTRAINT CHK_SourceCurrency CHECK (SourceCurrency IN ('USD', 'EUR', 'EUR')),
    CONSTRAINT CHK_TargetCurrency CHECK (TargetCurrency IN ('USD', 'EUR', 'EUR')),
    CONSTRAINT CHK_UnitCurrency CHECK (UnitCurrency IN ('USD', 'EUR', 'EUR')),
    CONSTRAINT CHK_InstructedCurrency CHECK (InstructedCurrency IN ('USD', 'EUR', 'EUR')),
    CONSTRAINT CHK_BalanceCreditDebitIndicator CHECK (BalanceCreditDebitIndicator IN ('Credit', 'Debit')),
    CONSTRAINT CHK_BalanceCurrency CHECK (BalanceCurrency IN ('USD', 'EUR', 'EUR')),
	CONSTRAINT CHK_ChargeCurrency CHECK (ChargeCurrency IN ('USD', 'EUR', 'EUR')),
    FOREIGN KEY (AccountId) REFERENCES accounts(AccountId)
);

INSERT INTO finance.person (Email, UserName, Password,CurrencyPreference) VALUES
('person1@example.com', 'user1', '$2b$12$YsyA7BgyKgzHVvxlMHSRtOxpncpzchK1EFIMZGmrrwbR0zlXcAIHu','EUR'), -- password1
('person2@example.com', 'user2', '$2a$12$DfrZ5NAdoVq1LSN8zNYAfOdD.Sj2qaVaCvQsUGLIWLCvYZbgKJlje','EUR'), -- password2
('person3@example.com', 'user3', '$2a$12$fZaCQxGiN2qFYDUA1pCzouAP2hYaSFB6XCVPYjMeQud3bhix0Sd/e','EUR'), -- password3
('person4@example.com', 'user4', '$2a$12$gDtvjf0dGhfY4NldonZ2Z.sGnfp/8iPiid4y5mpMyhEZC/R9WAfDW','EUR'), -- password4
('person5@example.com', 'user5', '$2a$12$EDTOUwW842pB4ysS2GyE9uJPByDbl75vxqZAMpDabhw7Px9dWbXE.','EUR'), -- password5
('person6@example.com', 'user6', '$2a$12$QluEA2rFA4VhJwqeQp0gJOOVev5gm86EAgq/0O7yAp9bAWs8.jwva','USD'), -- password6
('person7@example.com', 'user7', '$2a$12$Ad4G.L2Ut5VIoXb8avx.GO3UuYVWqAmuPKbEL.YlbYLgbDXVhllm.','USD'), -- password7
('person8@example.com', 'user8', '$2a$12$zFUuJsWgtu6xqdjZ/Vi8fu0v7lk9MJSOIoxAvt7OotziAPclVKXIy','USD'), -- password8
('person9@example.com', 'user9', '$2a$12$FkF1Gh0gjguI6u5aUDb04.x1WcDevpQK8JkDjJ98PhsLGjshoZMH.','USD'), -- password9
('person10@example.com', 'user10', '$2a$12$cFjybKaK9u/leA4qXCbfzuzq11e.0SAnAqyzrhS83Czi..PuZiiMe','USD'); -- password10

-- Insert Accounts for each Person
INSERT INTO finance.accounts (AccountId, Currency, AccountType, AccountSubType, Nickname, SchemeName, Identification, Name, SecondaryIdentification) VALUES
('ACC1001', 'USD', 'Personal', 'CurrentAccount', 'Person 1 Checking', 'US.RoutingNumberAccountNumber', '1234567890', 1, NULL),
('ACC1002', 'USD', 'Business', 'CurrentAccount', 'Person 1 Business', 'US.RoutingNumberAccountNumber', '0987654321', 1, NULL),
('ACC1003', 'EUR', 'Personal', 'Savings', 'Person 1 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201512345678', 1, NULL),
('ACC1004', 'EUR', 'Personal', 'Loan', 'Person 1 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201512345679', 1, NULL),
('ACC1005', 'USD', 'Personal', 'CurrentAccount', 'Person 2 Checking', 'US.RoutingNumberAccountNumber', '2345678901', 2, NULL),
('ACC1006', 'USD', 'Business', 'CurrentAccount', 'Person 2 Business', 'US.RoutingNumberAccountNumber', '8765432109', 2, NULL),
('ACC1007', 'EUR', 'Personal', 'Savings', 'Person 2 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201523456789', 2, NULL),
('ACC1008', 'EUR', 'Personal', 'Loan', 'Person 2 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201523456790', 2, NULL),
('ACC1009', 'USD', 'Personal', 'CurrentAccount', 'Person 3 Checking', 'US.RoutingNumberAccountNumber', '3456789012', 3, NULL),
('ACC1010', 'USD', 'Business', 'CurrentAccount', 'Person 3 Business', 'US.RoutingNumberAccountNumber', '7654321098', 3, NULL),
('ACC1011', 'EUR', 'Personal', 'Savings', 'Person 3 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201534567890', 3, NULL),
('ACC1012', 'EUR', 'Personal', 'Loan', 'Person 3 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201534567891', 3, NULL),
('ACC1013', 'USD', 'Personal', 'CurrentAccount', 'Person 4 Checking', 'US.RoutingNumberAccountNumber', '4567890123', 4, NULL),
('ACC1014', 'USD', 'Business', 'CurrentAccount', 'Person 4 Business', 'US.RoutingNumberAccountNumber', '6543210987', 4, NULL),
('ACC1015', 'EUR', 'Personal', 'Savings', 'Person 4 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201545678901', 4, NULL),
('ACC1016', 'EUR', 'Personal', 'Loan', 'Person 4 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201545678902', 4, NULL),
('ACC1017', 'USD', 'Personal', 'CurrentAccount', 'Person 5 Checking', 'US.RoutingNumberAccountNumber', '5678901234', 5, NULL),
('ACC1018', 'USD', 'Business', 'CurrentAccount', 'Person 5 Business', 'US.RoutingNumberAccountNumber', '5432109876', 5, NULL),
('ACC1019', 'EUR', 'Personal', 'Savings', 'Person 5 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201556789012', 5, NULL),
('ACC1020', 'EUR', 'Personal', 'Loan', 'Person 5 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201556789013', 5, NULL),
('ACC1021', 'USD', 'Personal', 'CurrentAccount', 'Person 6 Checking', 'US.RoutingNumberAccountNumber', '6789012345', 6, NULL),
('ACC1022', 'USD', 'Business', 'CurrentAccount', 'Person 6 Business', 'US.RoutingNumberAccountNumber', '4321098765', 6, NULL),
('ACC1023', 'EUR', 'Personal', 'Savings', 'Person 6 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201567890123', 6, NULL),
('ACC1024', 'EUR', 'Personal', 'Loan', 'Person 6 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201567890124', 6, NULL),
('ACC1025', 'USD', 'Personal', 'CurrentAccount', 'Person 7 Checking', 'US.RoutingNumberAccountNumber', '7890123456', 7, NULL),
('ACC1026', 'USD', 'Business', 'CurrentAccount', 'Person 7 Business', 'US.RoutingNumberAccountNumber', '3210987654', 7, NULL),
('ACC1027', 'EUR', 'Personal', 'Savings', 'Person 7 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201578901234', 7, NULL),
('ACC1028', 'EUR', 'Personal', 'Loan', 'Person 7 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201578901235', 7, NULL),
('ACC1029', 'USD', 'Personal', 'CurrentAccount', 'Person 8 Checking', 'US.RoutingNumberAccountNumber', '8901234567', 8, NULL),
('ACC1030', 'USD', 'Business', 'CurrentAccount', 'Person 8 Business', 'US.RoutingNumberAccountNumber', '2109876543', 8, NULL),
('ACC1031', 'EUR', 'Personal', 'Savings', 'Person 8 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201589012345', 8, NULL),
('ACC1032', 'EUR', 'Personal', 'Loan', 'Person 8 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201589012346', 8, NULL),
('ACC1033', 'USD', 'Personal', 'CurrentAccount', 'Person 9 Checking', 'US.RoutingNumberAccountNumber', '9012345678', 9, NULL),
('ACC1034', 'USD', 'Business', 'CurrentAccount', 'Person 9 Business', 'US.RoutingNumberAccountNumber', '1098765432', 9, NULL),
('ACC1035', 'EUR', 'Personal', 'Savings', 'Person 9 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201590123456', 9, NULL),
('ACC1036', 'EUR', 'Personal', 'Loan', 'Person 9 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201590123457', 9, NULL),
('ACC1037', 'USD', 'Personal', 'CurrentAccount', 'Person 10 Checking', 'US.RoutingNumberAccountNumber', '0123456789', 10, NULL),
('ACC1038', 'USD', 'Business', 'CurrentAccount', 'Person 10 Business', 'US.RoutingNumberAccountNumber', '0987654321', 10, NULL),
('ACC1039', 'EUR', 'Personal', 'Savings', 'Person 10 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201501234567', 10, NULL),
('ACC1040', 'EUR', 'Personal', 'Loan', 'Person 10 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201501234568', 10, NULL);
/* ('ACC2001', 'USD', 'Personal', 'CurrentAccount', 'Person 1 Checking', 'US.RoutingNumberAccountNumber', '1234567890', 1, NULL),
('ACC2002', 'USD', 'Business', 'CurrentAccount', 'Person 1 Business', 'US.RoutingNumberAccountNumber', '0987654321', 1, NULL),
('ACC2003', 'EUR', 'Personal', 'Savings', 'Person 1 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201512345678', 1, NULL),
('ACC2004', 'EUR', 'Personal', 'Loan', 'Person 1 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201512345679', 1, NULL),
('ACC2005', 'USD', 'Personal', 'CurrentAccount', 'Person 2 Checking', 'US.RoutingNumberAccountNumber', '2345678901', 2, NULL),
('ACC2006', 'USD', 'Business', 'CurrentAccount', 'Person 2 Business', 'US.RoutingNumberAccountNumber', '8765432109', 2, NULL),
('ACC2007', 'EUR', 'Personal', 'Savings', 'Person 2 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201523456789', 2, NULL),
('ACC2008', 'EUR', 'Personal', 'Loan', 'Person 2 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201523456790', 2, NULL),
('ACC2009', 'USD', 'Personal', 'CurrentAccount', 'Person 3 Checking', 'US.RoutingNumberAccountNumber', '3456789012', 3, NULL),
('ACC2010', 'USD', 'Business', 'CurrentAccount', 'Person 3 Business', 'US.RoutingNumberAccountNumber', '7654321098', 3, NULL),
('ACC2011', 'EUR', 'Personal', 'Savings', 'Person 3 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201534567890', 3, NULL),
('ACC2012', 'EUR', 'Personal', 'Loan', 'Person 3 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201534567891', 3, NULL),
('ACC2013', 'USD', 'Personal', 'CurrentAccount', 'Person 4 Checking', 'US.RoutingNumberAccountNumber', '4567890123', 4, NULL),
('ACC2014', 'USD', 'Business', 'CurrentAccount', 'Person 4 Business', 'US.RoutingNumberAccountNumber', '6543210987', 4, NULL),
('ACC2015', 'EUR', 'Personal', 'Savings', 'Person 4 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201545678901', 4, NULL),
('ACC2016', 'EUR', 'Personal', 'Loan', 'Person 4 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201545678902', 4, NULL),
('ACC2017', 'USD', 'Personal', 'CurrentAccount', 'Person 5 Checking', 'US.RoutingNumberAccountNumber', '5678901234', 5, NULL),
('ACC2018', 'USD', 'Business', 'CurrentAccount', 'Person 5 Business', 'US.RoutingNumberAccountNumber', '5432109876', 5, NULL),
('ACC2019', 'EUR', 'Personal', 'Savings', 'Person 5 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201556789012', 5, NULL),
('ACC2020', 'EUR', 'Personal', 'Loan', 'Person 5 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201556789013', 5, NULL),
('ACC2021', 'USD', 'Personal', 'CurrentAccount', 'Person 6 Checking', 'US.RoutingNumberAccountNumber', '6789012345', 6, NULL),
('ACC2022', 'USD', 'Business', 'CurrentAccount', 'Person 6 Business', 'US.RoutingNumberAccountNumber', '4321098765', 6, NULL),
('ACC2023', 'EUR', 'Personal', 'Savings', 'Person 6 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201567890123', 6, NULL),
('ACC2024', 'EUR', 'Personal', 'Loan', 'Person 6 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201567890124', 6, NULL),
('ACC2025', 'USD', 'Personal', 'CurrentAccount', 'Person 7 Checking', 'US.RoutingNumberAccountNumber', '7890123456', 7, NULL),
('ACC2026', 'USD', 'Business', 'CurrentAccount', 'Person 7 Business', 'US.RoutingNumberAccountNumber', '3210987654', 7, NULL),
('ACC2027', 'EUR', 'Personal', 'Savings', 'Person 7 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201578901234', 7, NULL),
('ACC2028', 'EUR', 'Personal', 'Loan', 'Person 7 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201578901235', 7, NULL),
('ACC2029', 'USD', 'Personal', 'CurrentAccount', 'Person 8 Checking', 'US.RoutingNumberAccountNumber', '8901234567', 8, NULL),
('ACC2030', 'USD', 'Business', 'CurrentAccount', 'Person 8 Business', 'US.RoutingNumberAccountNumber', '2109876543', 8, NULL),
('ACC2031', 'EUR', 'Personal', 'Savings', 'Person 8 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201589012345', 8, NULL),
('ACC2032', 'EUR', 'Personal', 'Loan', 'Person 8 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201589012346', 8, NULL),
('ACC2033', 'USD', 'Personal', 'CurrentAccount', 'Person 9 Checking', 'US.RoutingNumberAccountNumber', '9012345678', 9, NULL),
('ACC2034', 'USD', 'Business', 'CurrentAccount', 'Person 9 Business', 'US.RoutingNumberAccountNumber', '1098765432', 9, NULL),
('ACC2035', 'EUR', 'Personal', 'Savings', 'Person 9 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201590123456', 9, NULL),
('ACC2036', 'EUR', 'Personal', 'Loan', 'Person 9 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201590123457', 9, NULL),
('ACC2037', 'USD', 'Personal', 'CurrentAccount', 'Person 10 Checking', 'US.RoutingNumberAccountNumber', '0123456789', 10, NULL),
('ACC2038', 'USD', 'Business', 'CurrentAccount', 'Person 10 Business', 'US.RoutingNumberAccountNumber', '0987654321', 10, NULL),
('ACC2039', 'EUR', 'Personal', 'Savings', 'Person 10 Savings', 'UK.OBIE.IBAN', 'GB12BARC20201501234567', 10, NULL),
('ACC2040', 'EUR', 'Personal', 'Loan', 'Person 10 Loan', 'UK.OBIE.IBAN', 'GB12BARC20201501234568', 10, NULL); */

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
/* (1, 'ACC2001'),
(1, 'ACC2002'),
(2, 'ACC2003'),
(2, 'ACC2004'),
(3, 'ACC2005'),
(3, 'ACC2006'),
(4, 'ACC2007'),
(4, 'ACC2008'),
(5, 'ACC2009'),
(5, 'ACC2010'),
(1, 'ACC2011'),
(1, 'ACC2012'),
(2, 'ACC2013'),
(2, 'ACC2014'),
(3, 'ACC2015'),
(3, 'ACC2016'),
(4, 'ACC2017'),
(4, 'ACC2018'),
(5, 'ACC2019'),
(5, 'ACC2020'),
(1, 'ACC2021'),
(1, 'ACC2022'),
(2, 'ACC2023'),
(2, 'ACC2024'),
(3, 'ACC2025'),
(3, 'ACC2026'),
(4, 'ACC2027'),
(4, 'ACC2028'),
(5, 'ACC2029'),
(5, 'ACC2030'),
(1, 'ACC2031'),
(1, 'ACC2032'),
(2, 'ACC2033'),
(2, 'ACC2034'),
(3, 'ACC2035'),
(3, 'ACC2036'),
(4, 'ACC2037'),
(4, 'ACC2038'),
(5, 'ACC2039'),
(5, 'ACC2040');*/

-- Insert 3 Insurances per User
INSERT INTO finance.insurance (PolicyHolderId, InsuranceType, DateOpened, DateClosed, InsuranceState, PaymentAmount, PaymentUnitCurrency, InsuranceCompany, Description, Country, Frequency) VALUES
('ACC1001', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 150.50, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1002', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 200.25, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
('ACC1003', 'Life', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 250.75, 'EUR', 'AXA', 'Life Insurance', 'Germany', 'Monthly'),
('ACC1004', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 150.50, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1005', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 200.25, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
('ACC1006', 'Life', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 250.75, 'EUR', 'AXA', 'Life Insurance', 'Germany', 'Monthly'),
('ACC1007', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 150.50, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1008', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 200.25, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
('ACC1009', 'Life', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 250.75, 'EUR', 'AXA', 'Life Insurance', 'Germany', 'Monthly'),
('ACC1010', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 150.50, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Monthly'),
('ACC1011', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 200.25, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Monthly'),
--('ACC2001', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 1806.00, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Yearly');
--('ACC2002', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 2403.00, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Yearly'),
--('ACC2003', 'Life', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 3009.00, 'EUR', 'AXA', 'Life Insurance', 'Germany', 'Yearly'),
--('ACC2004', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 1806.00, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Yearly'),
--('ACC2005', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 2403.00, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Yearly'),
--('ACC2006', 'Life', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 3009.00, 'EUR', 'AXA', 'Life Insurance', 'Germany', 'Yearly'),
--('ACC2007', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 1806.00, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Yearly'),
--('ACC2008', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 2403.00, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Yearly'),
--('ACC2009', 'Life', '2022-06-15 08:30:00', '2023-06-15 08:30:00', FALSE, 3009.00, 'EUR', 'AXA', 'Life Insurance', 'Germany', 'Yearly'),
--('ACC2010', 'Health', '2022-01-01 10:00:00', NULL, TRUE, 1806.00, 'USD', 'Allianz', 'Health Insurance', 'USA', 'Yearly'),
--('ACC2011', 'Car', '2022-06-01 10:00:00', NULL, TRUE, 2403.00, 'USD', 'Progressive', 'Car Insurance', 'USA', 'Yearly');


-- Insert Loans per User (3 loans per user)
INSERT INTO finance.loan (CreditorAccountId, LoanType, LoanAmount, LoanUnitCurrency, InterestRate, InterestRateUnitCurrency, StartDate, EndDate, LoanStatus, Frequency, loanName, loanTerm, additionalCosts, effectiveInterestRate) VALUES
('ACC1001', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly','Mortage',7300,0.00,3.5),
('ACC1002', 'Car Loan', 20000.00, 'USD', 1.9, 'USD', '2021-10-01 09:00:00', '2025-10-01 09:00:00', 'Active', 'Monthly','Car',1825,0.00,1.9),
('ACC1003', 'Student Loan', 30000.00, 'USD', 2.5, 'USD', '2021-07-01 09:00:00', '2028-07-01 09:00:00', 'Active', 'Monthly',1825,3650,0.00,2.5),
('ACC1004', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly','Mortage',7300,0.00,3.5),
('ACC1005', 'Car Loan', 20000.00, 'USD', 1.9, 'USD', '2021-10-01 09:00:00', '2025-10-01 09:00:00', 'Active', 'Monthly','Car',3650,0.00,1.9),
('ACC1006', 'Student Loan', 30000.00, 'USD', 2.5, 'USD', '2021-07-01 09:00:00', '2028-07-01 09:00:00', 'Active', 'Monthly','Student',1825,0.00,2.5),
('ACC1007', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly','Mortage',7300,0.00,3.5),
('ACC1008', 'Car Loan', 20000.00, 'USD', 1.9, 'USD', '2021-10-01 09:00:00', '2025-10-01 09:00:00', 'Active', 'Monthly','Car',3650,0.00,1.9),
('ACC1009', 'Student Loan', 30000.00, 'USD', 2.5, 'USD', '2021-07-01 09:00:00', '2028-07-01 09:00:00', 'Active', 'Monthly','Student',1825,0.00,2.5),
('ACC1010', 'Mortgage', 250000.00, 'USD', 3.5, 'USD', '2020-07-01 09:00:00', '2030-07-01 09:00:00', 'Active', 'Monthly','Mortage',7300,0.00,3.5);

-- Insert 50 Standing Orders
INSERT INTO finance.standingOrders (CreditorAccountId, Frequency, NumberOfPayments, FirstPaymentDateTime, FinalPaymentDateTime, Reference, PaymentAmount, PaymentCurrency) VALUES
('ACC1001', 'Monthly', 12, '2023-02-01 10:00:00', '2024-02-01 10:00:00', 'Subscription Payment',15.00,'USD'),
('ACC1002', 'Quarterly', 8, '2023-01-15 12:00:00', '2024-01-15 12:00:00', 'Service Payment',300.00,'EUR'),
('ACC1003', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment',200.00,'EUR'),
('ACC1004', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment',2500.00,'EUR'),
('ACC1005', 'Monthly', 6, '2023-05-01 10:00:00', '2025-10-01 10:00:00', 'Subscription Payment',15.00,'USD'),
('ACC1006', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'USD'),
('ACC1007', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment',450.00,'EUR'),
('ACC1008', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment',275.00,'EUR'),
('ACC1009', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment',1040.00,'USD'),
('ACC1010', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'USD'),

('ACC1011', 'Monthly', 12, '2023-01-01 10:00:00', '2024-01-01 10:00:00', 'Subscription Payment',15.00,'EUR'),
('ACC1012', 'Quarterly', 8, '2023-02-15 12:00:00', '2024-02-15 12:00:00', 'Service Payment',300.00,'EUR'),
('ACC1013', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment',200.00,'USD'),
('ACC1014', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment',3000.00,'USD'),
('ACC1015', 'Monthly', 6, '2023-05-01 10:00:00', '2025-10-01 10:00:00', 'Subscription Payment',15.00,'EUR'),
('ACC1016', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'EUR'),
('ACC1017', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment',450.00,'USD'),
('ACC1018', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment',300.00,'USD'),
('ACC1019', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment',1040.00,'EUR'),
('ACC1020', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'EUR'),

('ACC1021', 'Monthly', 12, '2023-01-01 10:00:00', '2024-01-01 10:00:00', 'Subscription Payment',15.00,'USD'),
('ACC1022', 'Quarterly', 8, '2023-02-15 12:00:00', '2024-02-15 12:00:00', 'Service Payment',300.00,'USD'),
('ACC1023', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment',200.00,'EUR'),
('ACC1024', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment',3000.00,'EUR'),
('ACC1025', 'Monthly', 6, '2023-05-01 10:00:00', '2025-10-01 10:00:00', 'Subscription Payment',15.00,'USD'),
('ACC1026', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'USD'),
('ACC1027', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment',450.00,'EUR'),
('ACC1028', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment',300.00,'EUR'),
('ACC1029', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment',1040.00,'USD'),
('ACC1030', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'USD'),

('ACC1031', 'Monthly', 12, '2023-01-01 10:00:00', '2024-01-01 10:00:00', 'Subscription Payment',15.00,'EUR'),
('ACC1032', 'Quarterly', 8, '2023-02-15 12:00:00', '2024-02-15 12:00:00', 'Service Payment',300.00,'EUR'),
('ACC1033', 'Monthly', 12, '2023-03-01 10:00:00', '2024-03-01 10:00:00', 'Utility Payment',200.00,'USD'),
('ACC1034', 'Annually', 5, '2023-04-01 10:00:00', '2028-04-01 10:00:00', 'Tax Payment',3000.00,'USD'),
('ACC1035', 'Monthly', 6, '2023-05-01 10:00:00', '2025-10-01 10:00:00', 'Subscription Payment',15.00,'EUR'),
('ACC1036', 'Bi-Weekly', 10, '2023-06-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'EUR'),
('ACC1037', 'Monthly', 12, '2023-07-01 10:00:00', '2024-07-01 10:00:00', 'Insurance Payment',450.00,'USD'),
('ACC1038', 'Quarterly', 4, '2023-08-01 10:00:00', '2024-08-01 10:00:00', 'Service Payment',300.00,'USD'),
('ACC1039', 'Monthly', 24, '2023-09-01 10:00:00', '2025-09-01 10:00:00', 'Mortgage Payment',1040.00,'EUR'),
('ACC1040', 'Bi-Weekly', 10, '2023-10-01 10:00:00', '2025-11-01 10:00:00', 'Gym Membership Payment',10.00,'EUR');

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
('TXN002', 'ACC1001', 'Debit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 15.00, 'USD', 'T002', 'Issuer2', 'Subscription Payment', 'Netflix', 1.0, 'USD', 'USD', 'USD', 15.00, 'USD', 'Debit', 4985.00, 'USD', 0.00, 'USD', 'Data2'),
('TXN003', 'ACC1001', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 80.00, 'USD', 'T003', 'Issuer3', 'Grocery Purchase', 'Supermarket1', 1.0, 'USD', 'USD', 'USD', 80.00, 'USD', 'Debit', 4840.00, 'USD', 0.50, 'USD', 'Data3'),
('TXN004', 'ACC1001', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 25.00, 'USD', 'T004', 'Issuer4', 'Movie Ticket', 'Cinema1', 1.0, 'USD', 'USD', 'USD', 25.00, 'USD', 'Debit', 4815.00, 'USD', 0.25, 'USD', 'Data4'),
('TXN005', 'ACC1001', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 500.00, 'USD', 'T005', 'Issuer5', 'Bonus Payment', 'Employer2', 1.0, 'USD', 'USD', 'USD', 500.00, 'USD', 'Credit', 5315.00, 'USD', 1.00, 'USD', 'Data5'),
('TXN006', 'ACC1001', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 120.00, 'USD', 'T006', 'Issuer6', 'Restaurant Bill', 'Restaurant1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 5195.00, 'USD', 0.75, 'USD', 'Data6'),

('TXN007', 'ACC1002', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 180.00, 'EUR', 'T007', 'Issuer1', 'Consulting Fee', 'Client1', 0.9, 'EUR', 'USD', 'EUR', 180.00, 'EUR', 'Credit', 3500.00, 'EUR', 2.00, 'EUR', 'Data7'),
('TXN008', 'ACC1002', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 90.00, 'EUR', 'T008', 'Issuer2', 'Electricity Bill', 'Utility1', 0.9, 'EUR', 'USD', 'EUR', 90.00, 'EUR', 'Debit', 3410.00, 'EUR', 1.00, 'EUR', 'Data8'),
('TXN009', 'ACC1002', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 50.00, 'EUR', 'T009', 'Issuer3', 'Internet Bill', 'ISP1', 0.9, 'EUR', 'USD', 'EUR', 50.00, 'EUR', 'Debit', 3360.00, 'EUR', 0.75, 'EUR', 'Data9'),
('TXN010', 'ACC1002', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 300.00, 'EUR', 'T010', 'Issuer4', 'Freelance Project', 'Client2', 0.9, 'EUR', 'USD', 'EUR', 300.00, 'EUR', 'Credit', 3660.00, 'EUR', 1.50, 'EUR', 'Data10'),
('TXN011', 'ACC1002', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 70.00, 'EUR', 'T011', 'Issuer5', 'Rent Payment', 'Landlord1', 0.9, 'EUR', 'USD', 'EUR', 70.00, 'EUR', 'Debit', 3590.00, 'EUR', 0.50, 'EUR', 'Data11'),
('TXN012', 'ACC1002', 'Debit', 'Booked', '2024-04-15 12:00:00', '2024-04-15 12:00:00', 300.00, 'EUR', 'T012', 'Issuer6', 'Service Payment', 'Provider2', 1.0, 'EUR', 'USD', 'EUR', 300.00, 'EUR', 'Debit', 3290.00, 'EUR', 0.00, 'EUR', 'Data12'),

('TXN013', 'ACC1003', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 250.00, 'EUR', 'T013', 'Issuer1', 'Salary Payment', 'Employer1', 0.8, 'EUR', 'USD', 'EUR', 250.00, 'EUR', 'Credit', 4200.00, 'EUR', 2.50, 'EUR', 'Data13'),
('TXN014', 'ACC1003', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 200.00, 'EUR', 'T014', 'Issuer6', 'Utility Payment', 'UtilityCompany2', 0.9, 'EUR', 'USD', 'EUR', 200.00, 'EUR', 'Debit', 4000.00, 'EUR', 0.00, 'EUR', 'Data14'),
('TXN015', 'ACC1003', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 110.00, 'EUR', 'T015', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.8, 'EUR', 'USD', 'EUR', 110.00, 'EUR', 'Debit', 3890.00, 'EUR', 1.25, 'EUR', 'Data15'),
('TXN016', 'ACC1003', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 35.00, 'EUR', 'T016', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.8, 'EUR', 'USD', 'EUR', 35.00, 'EUR', 'Debit', 3855.00, 'EUR', 0.75, 'EUR', 'Data16'),
('TXN017', 'ACC1003', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 450.00, 'EUR', 'T017', 'Issuer4', 'Bonus Payment', 'Employer2', 0.8, 'EUR', 'USD', 'EUR', 450.00, 'EUR', 'Credit', 4305.00, 'EUR', 3.00, 'EUR', 'Data17'),
('TXN018', 'ACC1003', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 130.00, 'EUR', 'T018', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.8, 'EUR', 'USD', 'EUR', 130.00, 'EUR', 'Debit', 4175.00, 'EUR', 1.50, 'EUR', 'Data18'),

('TXN019', 'ACC1004', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 200.00, 'USD', 'T019', 'Issuer1', 'Consulting Fee', 'Client1', 1.0, 'USD', 'USD', 'USD', 200.00, 'USD', 'Credit', 4500.00, 'USD', 1.50, 'USD', 'Data19'),
('TXN020', 'ACC1004', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 2500.00, 'EUR', 'T020', 'Issuer6', 'Tax Payment', 'TaxOffice1', 1.0, 'EUR', 'USD', 'EUR', 2500.00, 'EUR', 'Debit', 2000.00, 'EUR', 0.00, 'EUR', 'Data20'),
('TXN021', 'ACC1004', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 120.00, 'USD', 'T021', 'Issuer2', 'Electricity Bill', 'Utility1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 1820.00, 'USD', 0.50, 'USD', 'Data21'),
('TXN022', 'ACC1004', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 60.00, 'USD', 'T022', 'Issuer3', 'Internet Bill', 'ISP1', 1.0, 'USD', 'USD', 'USD', 60.00, 'USD', 'Debit', 1760.00, 'USD', 0.25, 'USD', 'Data22'),
('TXN023', 'ACC1004', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 350.00, 'USD', 'T023', 'Issuer4', 'Freelance Project', 'Client2', 1.0, 'USD', 'USD', 'USD', 350.00, 'USD', 'Credit', 2110.00, 'USD', 0.75, 'USD', 'Data23'),
('TXN024', 'ACC1004', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 90.00, 'USD', 'T024', 'Issuer5', 'Rent Payment', 'Landlord1', 1.0, 'USD', 'USD', 'USD', 90.00, 'USD', 'Debit', 2020.00, 'USD', 0.25, 'USD', 'Data24'),

('TXN025', 'ACC1005', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 220.00, 'EUR', 'T025', 'Issuer1', 'Salary Payment', 'Employer1', 0.85, 'EUR', 'USD', 'EUR', 220.00, 'EUR', 'Credit', 3600.00, 'EUR', 2.00, 'EUR', 'Data25'),
('TXN026', 'ACC1005', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 15.00, 'USD', 'T026', 'Issuer6', 'Subscription Payment', 'Netflix', 1.0, 'USD', 'USD', 'USD', 15.00, 'USD', 'Debit', 3585.00, 'USD', 0.00, 'USD', 'Data26'),
('TXN027', 'ACC1005', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 100.00, 'EUR', 'T027', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.85, 'EUR', 'USD', 'EUR', 100.00, 'EUR', 'Debit', 3485.00, 'EUR', 0.75, 'EUR', 'Data27'),
('TXN028', 'ACC1005', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 40.00, 'EUR', 'T028', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.85, 'EUR', 'USD', 'EUR', 40.00, 'EUR', 'Debit', 3445.00, 'EUR', 0.25, 'EUR', 'Data28'),
('TXN029', 'ACC1005', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 400.00, 'EUR', 'T029', 'Issuer4', 'Bonus Payment', 'Employer2', 0.85, 'EUR', 'USD', 'EUR', 400.00, 'EUR', 'Credit', 3845.00, 'EUR', 1.50, 'EUR', 'Data29'),
('TXN030', 'ACC1005', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 110.00, 'EUR', 'T030', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.85, 'EUR', 'USD', 'EUR', 110.00, 'EUR', 'Debit', 3735.00, 'EUR', 0.75, 'EUR', 'Data30'),

('TXN031', 'ACC1006', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 150.00, 'USD', 'T031', 'Issuer1', 'Salary Payment', 'Employer1', 1.0, 'USD', 'USD', 'USD', 150.00, 'USD', 'Credit', 4200.00, 'USD', 2.00, 'USD', 'Data21'),
('TXN032', 'ACC1006', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 10.00, 'USD', 'T032', 'Issuer1', 'Gym Membership Payment', 'Gym1', 1.0, 'USD', 'USD', 'USD', 10.00, 'USD', 'Debit', 4190.00, 'USD', 0.00, 'USD', 'Data32'),
('TXN033', 'ACC1006', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 80.00, 'USD', 'T033', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 1.0, 'USD', 'USD', 'USD', 80.00, 'USD', 'Debit', 4110.00, 'USD', 1.00, 'USD', 'Data33'),
('TXN034', 'ACC1006', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 25.00, 'USD', 'T034', 'Issuer3', 'Movie Ticket', 'Cinema1', 1.0, 'USD', 'USD', 'USD', 25.00, 'USD', 'Debit', 4085.00, 'USD', 0.25, 'USD', 'Data34'),
('TXN035', 'ACC1006', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 500.00, 'USD', 'T035', 'Issuer4', 'Bonus Payment', 'Employer2', 1.0, 'USD', 'USD', 'USD', 500.00, 'USD', 'Credit', 4585.00, 'USD', 1.00, 'USD', 'Data35'),
('TXN036', 'ACC1006', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 120.00, 'USD', 'T036', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 4465.00, 'USD', 0.75, 'USD', 'Data36'),
('TXN037', 'ACC1006', 'Debit', 'Booked', '2024-04-15 10:00:00', '2024-04-15 10:00:00', 10.00, 'USD', 'T037', 'Issuer1', 'Gym Membership Payment', 'Gym1', 1.0, 'USD', 'USD', 'USD', 10.00, 'USD', 'Debit', 4455.00, 'USD', 0.00, 'USD', 'Data37'),

('TXN038', 'ACC1007', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 180.00, 'EUR', 'T038', 'Issuer1', 'Consulting Fee', 'Client1', 0.9, 'EUR', 'USD', 'EUR', 180.00, 'EUR', 'Credit', 3500.00, 'EUR', 2.00, 'EUR', 'Data38'),
('TXN039', 'ACC1007', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 450.00, 'EUR', 'T039', 'Issuer6', 'Insurance Payment', 'InsuranceCo1', 1.0, 'EUR', 'EUR', 'EUR', 450.00, 'EUR', 'Debit', 3050.00, 'EUR', 0.00, 'EUR', 'Data39'),
('TXN040', 'ACC1007', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 90.00, 'EUR', 'T040', 'Issuer2', 'Electricity Bill', 'Utility1', 0.9, 'EUR', 'USD', 'EUR', 90.00, 'EUR', 'Debit', 2960.00, 'EUR', 1.00, 'EUR', 'Data40'),
('TXN041', 'ACC1007', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 50.00, 'EUR', 'T041', 'Issuer3', 'Internet Bill', 'ISP1', 0.9, 'EUR', 'USD', 'EUR', 50.00, 'EUR', 'Debit', 2910.00, 'EUR', 0.75, 'EUR', 'Data41'),
('TXN042', 'ACC1007', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 300.00, 'EUR', 'T042', 'Issuer4', 'Freelance Project', 'Client2', 0.9, 'EUR', 'USD', 'EUR', 300.00, 'EUR', 'Credit', 3210.00, 'EUR', 1.50, 'EUR', 'Data42'),
('TXN043', 'ACC1007', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 70.00, 'EUR', 'T043', 'Issuer5', 'Rent Payment', 'Landlord1', 0.9, 'EUR', 'USD', 'EUR', 70.00, 'EUR', 'Debit', 3140.00, 'EUR', 0.50, 'EUR', 'Data43'),

('TXN044', 'ACC1008', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 250.00, 'EUR', 'T044', 'Issuer1', 'Salary Payment', 'Employer1', 0.8, 'EUR', 'USD', 'EUR', 250.00, 'EUR', 'Credit', 4200.00, 'EUR', 2.50, 'EUR', 'Data44'),
('TXN045', 'ACC1008', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 275.00, 'EUR', 'T045', 'Issuer6', 'Service Payment', 'ServiceCo1', 1.0, 'EUR', 'EUR', 'EUR', 550.00, 'EUR', 'Debit', 3650.00, 'EUR', 0.00, 'EUR', 'Data45'),
('TXN046', 'ACC1008', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 110.00, 'EUR', 'T046', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.8, 'EUR', 'USD', 'EUR', 110.00, 'EUR', 'Debit', 3540.00, 'EUR', 1.25, 'EUR', 'Data46'),
('TXN047', 'ACC1008', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 35.00, 'EUR', 'T047', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.8, 'EUR', 'USD', 'EUR', 35.00, 'EUR', 'Debit', 3505.00, 'EUR', 0.75, 'EUR', 'Data47'),
('TXN048', 'ACC1008', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 450.00, 'EUR', 'T048', 'Issuer4', 'Bonus Payment', 'Employer2', 0.8, 'EUR', 'USD', 'EUR', 450.00, 'EUR', 'Credit', 3955.00, 'EUR', 3.00, 'EUR', 'Data48'),
('TXN049', 'ACC1008', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 130.00, 'EUR', 'T049', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.8, 'EUR', 'USD', 'EUR', 130.00, 'EUR', 'Debit', 3825.00, 'EUR', 1.50, 'EUR', 'Data49'),

('TXN050', 'ACC1009', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 200.00, 'USD', 'T050', 'Issuer1', 'Consulting Fee', 'Client1', 1.0, 'USD', 'USD', 'USD', 200.00, 'USD', 'Credit', 4500.00, 'USD', 1.50, 'USD', 'Data50'),
('TXN051', 'ACC1009', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 1040.00, 'USD', 'T051', 'Issuer6', 'Mortgage Payment', 'Lender1', 1.0, 'USD', 'USD', 'USD', 1040.00, 'USD', 'Debit', 3460.00, 'USD', 0.00, 'USD', 'Data051'),
('TXN052', 'ACC1009', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 120.00, 'USD', 'T052', 'Issuer2', 'Electricity Bill', 'Utility1', 1.0, 'USD', 'USD', 'USD', 120.00, 'USD', 'Debit', 3340.00, 'USD', 0.50, 'USD', 'Data52'),
('TXN053', 'ACC1009', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 60.00, 'USD', 'T053', 'Issuer3', 'Internet Bill', 'ISP1', 1.0, 'USD', 'USD', 'USD', 60.00, 'USD', 'Debit', 3280.00, 'USD', 0.25, 'USD', 'Data53'),
('TXN054', 'ACC1009', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 350.00, 'USD', 'T054', 'Issuer4', 'Freelance Project', 'Client2', 1.0, 'USD', 'USD', 'USD', 350.00, 'USD', 'Credit', 3630.00, 'USD', 0.75, 'USD', 'Data54'),
('TXN055', 'ACC1009', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 90.00, 'USD', 'T055', 'Issuer5', 'Rent Payment', 'Landlord1', 1.0, 'USD', 'USD', 'USD', 90.00, 'USD', 'Debit', 3540.00, 'USD', 0.25, 'USD', 'Data55'),

('TXN056', 'ACC1010', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 220.00, 'EUR', 'T056', 'Issuer1', 'Salary Payment', 'Employer1', 0.85, 'EUR', 'USD', 'EUR', 220.00, 'EUR', 'Credit', 3600.00, 'EUR', 2.00, 'EUR', 'Data56'),
('TXN057', 'ACC1010', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 10.00, 'USD', 'T057', 'Issuer5', 'Gym Membership Payment', 'Gym1', 1.0, 'USD', 'USD', 'USD', 10.00, 'USD', 'Debit', 3590.00, 'USD', 0.00, 'USD', 'Data57'),
('TXN058', 'ACC1010', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 100.00, 'EUR', 'T058', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.85, 'EUR', 'USD', 'EUR', 100.00, 'EUR', 'Debit', 3490.00, 'EUR', 0.75, 'EUR', 'Data58'),
('TXN059', 'ACC1010', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 40.00, 'EUR', 'T059', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.85, 'EUR', 'USD', 'EUR', 40.00, 'EUR', 'Debit', 3450.00, 'EUR', 0.25, 'EUR', 'Data59'),
('TXN060', 'ACC1010', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 400.00, 'EUR', 'T060', 'Issuer4', 'Bonus Payment', 'Employer2', 0.85, 'EUR', 'USD', 'EUR', 400.00, 'EUR', 'Credit', 3850.00, 'EUR', 1.50, 'EUR', 'Data60'),
('TXN061', 'ACC1010', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 110.00, 'EUR', 'T061', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.85, 'EUR', 'USD', 'EUR', 110.00, 'EUR', 'Debit', 3750.00, 'EUR', 0.75, 'EUR', 'Data61'),
('TXN062', 'ACC1010', 'Debit', 'Booked', '2024-04-15 10:00:00', '2024-04-15 10:00:00', 10.00, 'USD', 'T062', 'Issuer5', 'Gym Membership Payment', 'Gym1', 1.0, 'USD', 'USD', 'USD', 10.00, 'USD', 'Debit', 3730.00, 'USD', 0.00, 'USD', 'Data62'),

('TXN063', 'ACC1011', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 180.00, 'USD', 'T063', 'Issuer1', 'Consulting Fee', 'Client1', 1.0, 'USD', 'USD', 'USD', 180.00, 'USD', 'Credit', 4200.00, 'USD', 2.00, 'USD', 'Data63'),
('TXN064', 'ACC1011', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 15.00, 'EUR', 'T064', 'Issuer6', 'Subscription Payment', 'Netflix', 1.0, 'EUR', 'EUR', 'EUR', 15.00, 'EUR', 'Debit', 4185.00, 'EUR', 0.00, 'EUR', 'Data64'),
('TXN065', 'ACC1011', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 90.00, 'USD', 'T065', 'Issuer2', 'Electricity Bill', 'Utility1', 1.0, 'USD', 'USD', 'USD', 90.00, 'USD', 'Debit', 4095.00, 'USD', 1.00, 'USD', 'Data65'),
('TXN066', 'ACC1011', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 50.00, 'USD', 'T066', 'Issuer3', 'Internet Bill', 'ISP1', 1.0, 'USD', 'USD', 'USD', 50.00, 'USD', 'Debit', 4045.00, 'USD', 0.75, 'USD', 'Data66'),
('TXN067', 'ACC1011', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 300.00, 'USD', 'T067', 'Issuer4', 'Freelance Project', 'Client2', 1.0, 'USD', 'USD', 'USD', 300.00, 'USD', 'Credit', 4345.00, 'USD', 1.50, 'USD', 'Data67'),
('TXN068', 'ACC1011', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 70.00, 'USD', 'T068', 'Issuer5', 'Rent Payment', 'Landlord1', 1.0, 'USD', 'USD', 'USD', 70.00, 'USD', 'Debit', 4275.00, 'USD', 0.50, 'USD', 'Data68'),

('TXN069', 'ACC1012', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 250.00, 'EUR', 'T069', 'Issuer1', 'Salary Payment', 'Employer1', 0.8, 'EUR', 'USD', 'EUR', 250.00, 'EUR', 'Credit', 3500.00, 'EUR', 2.00, 'EUR', 'Data69'),
('TXN070', 'ACC1012', 'Debit', 'Booked', '2024-04-01 12:00:00', '2024-04-01 12:00:00', 300.00, 'EUR', 'T070', 'Issuer6', 'Service Payment', 'ServiceCo1', 1.0, 'EUR', 'EUR', 'EUR', 300.00, 'EUR', 'Debit', 3200.00, 'EUR', 0.00, 'EUR', 'Data70'),
('TXN071', 'ACC1012', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 120.00, 'EUR', 'T071', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 0.8, 'EUR', 'USD', 'EUR', 120.00, 'EUR', 'Debit', 3080.00, 'EUR', 1.25, 'EUR', 'Data71'),
('TXN072', 'ACC1012', 'Debit', 'Pending', '2024-04-03 08:30:00', '2024-04-03 08:30:00', 60.00, 'EUR', 'T072', 'Issuer3', 'Movie Ticket', 'Cinema1', 0.8, 'EUR', 'USD', 'EUR', 60.00, 'EUR', 'Debit', 3020.00, 'EUR', 0.75, 'EUR', 'Data72'),
('TXN073', 'ACC1012', 'Credit', 'Booked', '2024-04-04 14:15:00', '2024-04-04 14:15:00', 400.00, 'EUR', 'T073', 'Issuer4', 'Bonus Payment', 'Employer2', 0.8, 'EUR', 'USD', 'EUR', 400.00, 'EUR', 'Credit', 3420.00, 'EUR', 2.50, 'EUR', 'Data73'),
('TXN074', 'ACC1012', 'Debit', 'Booked', '2024-04-05 18:45:00', '2024-04-05 18:45:00', 110.00, 'EUR', 'T074', 'Issuer5', 'Restaurant Bill', 'Restaurant1', 0.8, 'EUR', 'USD', 'EUR', 110.00, 'EUR', 'Debit', 3310.00, 'EUR', 0.75, 'EUR', 'Data74'),

('TXN075', 'ACC1013', 'Credit', 'Booked', '2024-04-01 10:00:00', '2024-04-01 10:00:00', 150.00, 'USD', 'T075', 'Issuer1', 'Salary Payment', 'Employer1', 1.0, 'USD', 'USD', 'USD', 150.00, 'USD', 'Credit', 3500.00, 'USD', 2.00, 'USD', 'Data75'),
('TXN076', 'ACC1013', 'Debit', 'Booked', '2024-04-01 11:00:00', '2024-04-01 11:00:00', 200.00, 'USD', 'T076', 'Issuer6', 'Utility Payment', 'UtilityCo1', 1.0, 'USD', 'USD', 'USD', 200.00, 'USD', 'Debit', 3300.00, 'USD', 0.00, 'USD', 'Data76'),
('TXN077', 'ACC1013', 'Debit', 'Booked', '2024-04-02 12:00:00', '2024-04-02 12:00:00', 80.00, 'USD', 'T077', 'Issuer2', 'Grocery Purchase', 'Supermarket1', 1.0, 'USD', 'USD', 'USD', 80.00, 'USD', 'Debit', 3220.00, 'USD', 1.00, 'USD', 'Data77');

-- Transactions for Standingorders