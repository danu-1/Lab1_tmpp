namespace DentalClinic.Enums
{
    // Statusul unei programări
    public enum AppointmentStatus
    {
        Scheduled,
        Confirmed,
        Completed,
        Cancelled,
        NoShow
    }

    // Tipul de tratament stomatologic
    public enum TreatmentType
    {
        Consultation,
        Cleaning,
        Filling,
        RootCanal,
        Extraction,
        Whitening,
        Orthodontics,
        Implant,
        CrownOrBridge
    }

    // Metoda de plată
    public enum PaymentMethod
    {
        Cash,
        Card,
        BankTransfer,
        Insurance
    }

    // Statusul plății
    public enum PaymentStatus
    {
        Pending,
        Paid,
        PartiallyPaid,
        Refunded,
        Overdue
    }

    // Specializarea medicului
    public enum DoctorSpecialization
    {
        GeneralDentist,
        Orthodontist,
        Endodontist,
        Periodontist,
        OralSurgeon,
        Prosthodontist
    }
}
