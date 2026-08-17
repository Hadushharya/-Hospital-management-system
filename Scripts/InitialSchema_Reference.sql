-- ============================================================================
-- REFERENCE ONLY — this is NOT the actual EF Core migration.
-- Generate the real migration on your machine instead:
--   cd HospitalManagementSystem
--   dotnet ef migrations add InitialCreate
--   dotnet ef database update
-- Identity tables (AspNetUsers, AspNetRoles, etc.) are omitted — EF Identity
-- generates those automatically. All primary keys are INT IDENTITY (auto-increment).
-- ============================================================================

CREATE TABLE Suppliers (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Contact NVARCHAR(200) NOT NULL
);

CREATE TABLE FeeSettings (
    Id INT IDENTITY PRIMARY KEY,          -- doubles as the "payment order id"
    PaymentName NVARCHAR(200) NOT NULL,
    FeeType INT NOT NULL,                 -- 0 Registration, 1 Consultation, 2 Lab
    Amount DECIMAL(10,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Patients (
    Id INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(30) NOT NULL,
    Gender NVARCHAR(20) NOT NULL,
    Age NVARCHAR(20) NULL,
    Wereda NVARCHAR(100) NULL,
    Kebele NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL
);

CREATE TABLE Visits (
    Id INT IDENTITY PRIMARY KEY,
    PatientId INT NOT NULL REFERENCES Patients(Id),
    VisitDate DATETIME2 NOT NULL,
    Status INT NOT NULL
);

CREATE TABLE Triages (
    Id INT IDENTITY PRIMARY KEY,
    VisitId INT NOT NULL UNIQUE REFERENCES Visits(Id),
    Vitals NVARCHAR(500) NOT NULL,
    Notes NVARCHAR(1000) NOT NULL,
    RecordedAt DATETIME2 NOT NULL
);

CREATE TABLE Consultations (
    Id INT IDENTITY PRIMARY KEY,
    VisitId INT NOT NULL REFERENCES Visits(Id),
    DoctorId NVARCHAR(450) NOT NULL,
    Diagnosis NVARCHAR(1000) NOT NULL,
    Notes NVARCHAR(1000) NOT NULL,
    ConsultationDate DATETIME2 NOT NULL
);

CREATE TABLE Invoices (
    Id INT IDENTITY PRIMARY KEY,
    VisitId INT NOT NULL REFERENCES Visits(Id),
    FeeSettingId INT NULL REFERENCES FeeSettings(Id),
    FeeType INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Status INT NOT NULL,
    PaymentMethod INT NOT NULL,
    CollectedByUserId NVARCHAR(450) NULL,
    CreatedAt DATETIME2 NOT NULL
);

CREATE TABLE LabRequests (
    Id INT IDENTITY PRIMARY KEY,
    ConsultationId INT NOT NULL REFERENCES Consultations(Id),
    TestName NVARCHAR(200) NOT NULL,
    Status INT NOT NULL,
    InvoiceId INT NULL UNIQUE REFERENCES Invoices(Id),
    RequestedAt DATETIME2 NOT NULL
);

CREATE TABLE LabResults (
    Id INT IDENTITY PRIMARY KEY,
    LabRequestId INT NOT NULL UNIQUE REFERENCES LabRequests(Id),
    ResultValue NVARCHAR(1000) NOT NULL,
    Notes NVARCHAR(1000) NOT NULL,
    TechnicianId NVARCHAR(450) NOT NULL,
    ResultDate DATETIME2 NOT NULL
);

CREATE TABLE Prescriptions (
    Id INT IDENTITY PRIMARY KEY,
    ConsultationId INT NOT NULL REFERENCES Consultations(Id),
    Status INT NOT NULL
);

CREATE TABLE Payments (
    Id INT IDENTITY PRIMARY KEY,
    PrescriptionId INT NOT NULL UNIQUE REFERENCES Prescriptions(Id),
    Amount DECIMAL(10,2) NOT NULL,
    Status INT NOT NULL,
    PaymentMethod INT NOT NULL,
    CashierId NVARCHAR(450) NOT NULL,
    PaidAt DATETIME2 NOT NULL
);

CREATE TABLE Medicines (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    BatchNo NVARCHAR(100) NOT NULL,
    ExpiryDate DATE NOT NULL,
    CostPrice DECIMAL(10,2) NOT NULL,
    SellingPrice DECIMAL(10,2) NOT NULL,
    DosageForm NVARCHAR(100) NOT NULL,
    ControlledSubstance BIT NOT NULL,
    QuantityInStock INT NOT NULL,
    ReorderLevel INT NOT NULL,
    SupplierId INT NULL REFERENCES Suppliers(Id),
    RegisteredByUserId NVARCHAR(450) NOT NULL,
    DateReceived DATETIME2 NOT NULL
);

CREATE TABLE MedicalGoods (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    BatchNo NVARCHAR(100) NOT NULL,
    ExpiryDate DATE NOT NULL,
    CostPrice DECIMAL(10,2) NOT NULL,
    SellingPrice DECIMAL(10,2) NOT NULL,
    UnitOfMeasure NVARCHAR(50) NOT NULL,
    QuantityInStock INT NOT NULL,
    ReorderLevel INT NOT NULL,
    SupplierId INT NULL REFERENCES Suppliers(Id),
    RegisteredByUserId NVARCHAR(450) NOT NULL,
    DateReceived DATETIME2 NOT NULL
);

CREATE TABLE PrescriptionItems (
    Id INT IDENTITY PRIMARY KEY,
    PrescriptionId INT NOT NULL REFERENCES Prescriptions(Id),
    MedicineId INT NOT NULL REFERENCES Medicines(Id),
    Quantity INT NOT NULL
);

CREATE TABLE StockTransactions (
    Id INT IDENTITY PRIMARY KEY,
    ItemType INT NOT NULL,
    ItemId INT NOT NULL,
    TransactionType INT NOT NULL,
    Quantity INT NOT NULL,
    ReferenceNote NVARCHAR(500) NULL,
    StaffId NVARCHAR(450) NOT NULL,
    Timestamp DATETIME2 NOT NULL
);
CREATE INDEX IX_StockTransactions_ItemType_ItemId ON StockTransactions (ItemType, ItemId);
