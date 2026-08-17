namespace HospitalManagementSystem.Models;

public enum VisitStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

public enum FeeType
{
    Registration,
    Consultation,
    Lab
}

public enum InvoiceStatus
{
    Pending,
    Paid,
    Cancelled,
    Refunded
}

public enum LabRequestStatus
{
    Requested,          // doctor just ordered it
    PaymentPending,      // sent to reception, awaiting payment
    PaymentConfirmed,    // reception collected fee, patient sent to lab
    InProgress,          // lab technician processing sample
    Completed,      // results entered
    ResultEntered   // technician saved a result, not yet sent to the doctor
}

public enum PrescriptionStatus
{
    Pending,    // written by doctor, awaiting cashier payment
    Paid,       // cashier collected payment
    Dispensed,  // pharmacist gave out the medicine (triggers stock deduction)
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Cancelled,
    Refunded
}

public enum StockItemType
{
    Medicine,
    MedicalGood
}

public enum StockTransactionType
{
    StockIn,      // Store Man receiving new stock
    StockOut,     // Pharmacist dispensing (auto-deducted)
    Adjustment    // manual correction after a stock check
}

public enum PaymentMethod
{
    Cash,
    BankTransfer
}

// Fixed staff roles, used for seeding Identity roles and for [Authorize(Roles = "...")].
public static class StaffRoles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string TriageNurse = "TriageNurse";
    public const string LabTechnician = "LabTechnician";
    public const string StoreMan = "StoreMan";
    public const string Pharmacist = "Pharmacist";
    public const string Cashier = "Cashier";

    // Replaces the single generic "Doctor" role — each OPD type is its own
    // role/login, but all four get OPD/consultation access (see OpdRolesCsv).
    public const string EmergencyOPD = "EmergencyOPD";
    public const string AdultOPD = "AdultOPD";
    public const string ChildOPD = "ChildOPD";
    public const string FamilyPlanningOPD = "FamilyPlanningOPD";

    // const string concatenation is a compile-time constant, so this can be used
    // directly inside [Authorize(Roles = StaffRoles.OpdRolesCsv + "," + StaffRoles.Admin)]
    public const string OpdRolesCsv = EmergencyOPD + "," + AdultOPD + "," + ChildOPD + "," + FamilyPlanningOPD;

    public static readonly string[] OpdRoles = { EmergencyOPD, AdultOPD, ChildOPD, FamilyPlanningOPD };

    public static readonly string[] All =
    {
        Admin, Receptionist, TriageNurse, LabTechnician, StoreMan, Pharmacist, Cashier,
        EmergencyOPD, AdultOPD, ChildOPD, FamilyPlanningOPD
    };
}