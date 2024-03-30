-- Created by Vertabelo (http://vertabelo.com)
-- Last modification date: 2024-03-29 13:18:36.888

-- tables
-- Table: account
use finance;

CREATE TABLE finance.account (
    AccountID int  NOT NULL,
    AccountNumber varchar(20)  NOT NULL,
    AccountType varchar(20)  NOT NULL,
    CurrentBalance decimal(10,2)  NOT NULL,
    DateOpened date  NOT NULL,
    DateClosed date  NULL,
    AccountState boolean  NOT NULL,
    person_personID int  NOT NULL,
    bank_bankID int  NOT NULL,
    UNIQUE INDEX account_ak_1 (AccountType,AccountNumber),
    CONSTRAINT account_pk PRIMARY KEY (AccountID)
) COMMENT 'Keeps information about the different accounts each customer or group of customers can have in the bank';

-- Table: bank
CREATE TABLE finance.bank (
    bankID int  NOT NULL,
    displayName varchar(50)  NOT NULL,
    description varchar(50)  NOT NULL,
    country varchar(50)  NOT NULL,
    BIC int  NOT NULL,
    orderNumber bigint  NOT NULL COMMENT 'Verfügernummer',
    orderNumberPW int  NOT NULL,
    CONSTRAINT bank_pk PRIMARY KEY (bankID)
);

-- Table: files
CREATE TABLE finance.files (
    fileID int  NOT NULL,
    fileType varchar(20)  NOT NULL,
    fileInfo longblob  NOT NULL,
    loan_loanID int  NOT NULL,
    insurance_insuranceID int  NOT NULL,
    transaction_transactionID int  NOT NULL,
    CONSTRAINT files_pk PRIMARY KEY (fileID)
);

-- Table: insurance
CREATE TABLE finance.insurance (
    insuranceID int  NOT NULL,
    insuranceType varchar(30)  NOT NULL,
    paymentInstalment decimal(10,2)  NOT NULL,
    paymentInstalmentUnit varchar(5)  NOT NULL,
    dateOpened datetime  NOT NULL,
    insuranceState boolean  NOT NULL,
    paymentAmount decimal(10,2)  NOT NULL,
    dateClosed datetime  NULL,
    paymentUnit varchar(5)  NOT NULL,
    polizze longblob  NOT NULL,
    insuranceCompany_insuranceCompanyID int  NOT NULL,
    person_personID int  NOT NULL,
    transaction_transactionID int  NOT NULL,
    CONSTRAINT insurance_pk PRIMARY KEY (insuranceID)
);

-- Table: insuranceCompany
CREATE TABLE finance.insuranceCompany (
    insuranceCompanyID int  NOT NULL,
    insuranceCompany varchar(20)  NOT NULL,
    description varchar(20)  NULL,
    country varchar(10)  NOT NULL,
    CONSTRAINT insuranceCompany_pk PRIMARY KEY (insuranceCompanyID)
);

-- Table: loan
CREATE TABLE finance.loan (
    loanID int  NOT NULL,
    loanType varchar(20)  NOT NULL,
    loanAmount decimal(10,2)  NOT NULL,
    loanUnit varchar(5)  NOT NULL,
    interestRate decimal(10,2)  NOT NULL,
    interestRateUnit varchar(5)  NOT NULL,
    startDate date  NOT NULL,
    endDate date  NULL,
    loanStatus varchar(20)  NOT NULL,
    paymentInterval varchar(20)  NOT NULL COMMENT 'Zahlungsinterval',
    CONSTRAINT loan_pk PRIMARY KEY (loanID)
) COMMENT 'Keeps information about the different loans that the bank grants to customers';

-- Table: loanPayment
CREATE TABLE finance.loanPayment (
    loanPaymentID int  NOT NULL,
    scheduledPaymentDate date  NOT NULL,
    paymentAmount decimal(10,2)  NOT NULL,
    principalAmount decimal(10,2)  NOT NULL,
    interestAmount decimal(10,2)  NOT NULL,
    paidAmount decimal(10,2)  NOT NULL,
    paidDate date  NOT NULL,
    paymentType varchar(20)  NOT NULL,
    loan_loanID int  NOT NULL,
    CONSTRAINT loanPayment_pk PRIMARY KEY (loanPaymentID)
) COMMENT 'Keeps information about each scheduled Loan Payment';

-- Table: person
CREATE TABLE finance.person (
    personID int  NOT NULL,
    email varchar(100)  NOT NULL,
    userName varchar(20)  NOT NULL,
    password int  NOT NULL,
    CONSTRAINT person_pk PRIMARY KEY (personID)
) COMMENT 'Keeps information about each person that interacts with the bank';

-- Table: person_loan
CREATE TABLE finance.person_loan (
    person_personID int  NOT NULL,
    loan_loanID int  NOT NULL,
    CONSTRAINT person_loan_pk PRIMARY KEY (person_personID,loan_loanID)
);

-- Table: savingPlan
CREATE TABLE finance.savingPlan (
    savingPlanID int  NOT NULL,
    targetGoal varchar(20)  NOT NULL,
    targetAmount decimal(10,2)  NOT NULL,
    targetAmountUnit varchar(5)  NOT NULL,
    currentAmount decimal(10,2)  NOT NULL,
    currentAmountUnit varchar(5)  NOT NULL,
    openDate datetime  NOT NULL,
    closedDate datetime  NULL,
    state boolean  NOT NULL,
    paymentInterval varchar(20)  NOT NULL COMMENT 'monatlich, jährlich, wöchentlich, quartalsweise',
    person_personID int  NOT NULL,
    bank_bankID int  NOT NULL,
    insuranceCompany_insuranceCompanyID int  NOT NULL,
    transaction_transactionID int  NOT NULL,
    CONSTRAINT savingPlan_pk PRIMARY KEY (savingPlanID)
);

-- Table: transaction
CREATE TABLE finance.transaction (
    transactionID int  NOT NULL,
    transactionType varchar(20)  NOT NULL,
    amount decimal(10,2)  NOT NULL,
    amountUnit varchar(5)  NOT NULL,
    transactionDate datetime  NOT NULL,
    person_personID int  NOT NULL,
    account_AccountID int  NOT NULL,
    loanPayment_loanPaymentID int  NOT NULL,
    CONSTRAINT transaction_pk PRIMARY KEY (transactionID)
) COMMENT 'Keeps information about every transaction performed on the Bank';

-- foreign keys
-- Reference: account_bank (table: account)
ALTER TABLE finance.account ADD CONSTRAINT account_bank FOREIGN KEY account_bank (bank_bankID)
    REFERENCES finance.bank (bankID);

-- Reference: files_insurance (table: files)
ALTER TABLE finance.files ADD CONSTRAINT files_insurance FOREIGN KEY files_insurance (insurance_insuranceID)
    REFERENCES finance.insurance (insuranceID);

-- Reference: files_loan (table: files)
ALTER TABLE finance.files ADD CONSTRAINT files_loan FOREIGN KEY files_loan (loan_loanID)
    REFERENCES finance.loan (loanID);

-- Reference: files_transaction (table: files)
ALTER TABLE finance.files ADD CONSTRAINT files_transaction FOREIGN KEY files_transaction (transaction_transactionID)
    REFERENCES finance.transaction (transactionID);

-- Reference: insurance_insuranceCompany (table: insurance)
ALTER TABLE finance.insurance ADD CONSTRAINT insurance_insuranceCompany FOREIGN KEY insurance_insuranceCompany (insuranceCompany_insuranceCompanyID)
    REFERENCES finance.insuranceCompany (insuranceCompanyID);

-- Reference: insurance_transaction (table: insurance)
ALTER TABLE finance.insurance ADD CONSTRAINT insurance_transaction FOREIGN KEY insurance_transaction (transaction_transactionID)
    REFERENCES finance.transaction (transactionID);

-- Reference: loanPayment_loan (table: loanPayment)
ALTER TABLE finance.loanPayment ADD CONSTRAINT loanPayment_loan FOREIGN KEY loanPayment_loan (loan_loanID)
    REFERENCES finance.loan (loanID);

-- Reference: person_account (table: account)
ALTER TABLE finance.account ADD CONSTRAINT person_account FOREIGN KEY person_account (person_personID)
    REFERENCES finance.person (personID);

-- Reference: person_insurance (table: insurance)
ALTER TABLE finance.insurance ADD CONSTRAINT person_insurance FOREIGN KEY person_insurance (person_personID)
    REFERENCES finance.person (personID);

-- Reference: person_loan_loan (table: person_loan)
ALTER TABLE finance.person_loan ADD CONSTRAINT person_loan_loan FOREIGN KEY person_loan_loan (loan_loanID)
    REFERENCES finance.loan (loanID);

-- Reference: person_loan_person (table: person_loan)
ALTER TABLE finance.person_loan ADD CONSTRAINT person_loan_person FOREIGN KEY person_loan_person (person_personID)
    REFERENCES finance.person (personID);

-- Reference: savingPlan_bank (table: savingPlan)
ALTER TABLE finance.savingPlan ADD CONSTRAINT savingPlan_bank FOREIGN KEY savingPlan_bank (bank_bankID)
    REFERENCES finance.bank (bankID);

-- Reference: savingPlan_insuranceCompany (table: savingPlan)
ALTER TABLE finance.savingPlan ADD CONSTRAINT savingPlan_insuranceCompany FOREIGN KEY savingPlan_insuranceCompany (insuranceCompany_insuranceCompanyID)
    REFERENCES finance.insuranceCompany (insuranceCompanyID);

-- Reference: savingPlan_person (table: savingPlan)
ALTER TABLE finance.savingPlan ADD CONSTRAINT savingPlan_person FOREIGN KEY savingPlan_person (person_personID)
    REFERENCES finance.person (personID);

-- Reference: savingPlan_transaction (table: savingPlan)
ALTER TABLE finance.savingPlan ADD CONSTRAINT savingPlan_transaction FOREIGN KEY savingPlan_transaction (transaction_transactionID)
    REFERENCES finance.transaction (transactionID);

-- Reference: transaction_account (table: transaction)
ALTER TABLE finance.transaction ADD CONSTRAINT transaction_account FOREIGN KEY transaction_account (account_AccountID)
    REFERENCES finance.account (AccountID);

-- Reference: transaction_loanPayment (table: transaction)
ALTER TABLE finance.transaction ADD CONSTRAINT transaction_loanPayment FOREIGN KEY transaction_loanPayment (loanPayment_loanPaymentID)
    REFERENCES finance.loanPayment (loanPaymentID);

-- Reference: transaction_person (table: transaction)
ALTER TABLE finance.transaction ADD CONSTRAINT transaction_person FOREIGN KEY transaction_person (person_personID)
    REFERENCES finance.person (personID);

-- End of file.

