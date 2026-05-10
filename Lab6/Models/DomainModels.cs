using System;
using System.Collections.Generic;

namespace DentalClinic.Models
{
    // ─── Enums ────────────────────────────────────────────────────────────────

    public enum AppointmentStatus
    {
        Scheduled,
        Confirmed,
        InProgress,
        Completed,
        Cancelled,
        Rescheduled
    }

    public enum PatientCategory { Standard, VIP, Insured }

    // ─── Patient ──────────────────────────────────────────────────────────────

    public class Patient
    {
        public string Id        { get; set; }
        public string Name      { get; set; }
        public string Phone     { get; set; }
        public string Email     { get; set; }
        public DateTime BirthDate { get; set; }
        public PatientCategory Category { get; set; }
        public string InsurancePolicyNumber { get; set; }
        public double InsuranceCoverage { get; set; }   // 0.0 – 1.0

        public Patient(string id, string name, string phone, string email,
                       DateTime birthDate, PatientCategory category = PatientCategory.Standard)
        {
            Id        = id;
            Name      = name;
            Phone     = phone;
            Email     = email;
            BirthDate = birthDate;
            Category  = category;
        }

        public override string ToString() =>
            $"[{Id}] {Name} | {Category} | {Phone}";
    }

    // ─── Doctor ───────────────────────────────────────────────────────────────

    public class Doctor
    {
        public string Id          { get; set; }
        public string Name        { get; set; }
        public string Speciality  { get; set; }
        public string Email       { get; set; }

        public Doctor(string id, string name, string speciality, string email = "")
        {
            Id         = id;
            Name       = name;
            Speciality = speciality;
            Email      = email;
        }

        public override string ToString() => $"Dr. {Name} ({Speciality})";
    }

    // ─── Appointment ─────────────────────────────────────────────────────────

    public class Appointment
    {
        public string            Id          { get; set; }
        public Patient           Patient     { get; set; }
        public Doctor            Doctor      { get; set; }
        public DateTime          DateTime    { get; set; }
        public string            Service     { get; set; }
        public double            BasePrice   { get; set; }
        public AppointmentStatus Status      { get; set; }
        public string            Notes       { get; set; }

        public Appointment(string id, Patient patient, Doctor doctor,
                           DateTime dateTime, string service, double basePrice)
        {
            Id        = id;
            Patient   = patient;
            Doctor    = doctor;
            DateTime  = dateTime;
            Service   = service;
            BasePrice = basePrice;
            Status    = AppointmentStatus.Scheduled;
            Notes     = string.Empty;
        }

        public Appointment Clone()
        {
            return new Appointment(Id, Patient, Doctor, DateTime, Service, BasePrice)
            {
                Status = this.Status,
                Notes  = this.Notes
            };
        }

        public override string ToString() =>
            $"[{Id}] {Patient.Name} → {Doctor} | {Service} | {DateTime:dd.MM.yyyy HH:mm} | {Status}";
    }

    // ─── TreatmentRecord ──────────────────────────────────────────────────────

    public class TreatmentRecord
    {
        public string   Id          { get; set; }
        public string   PatientId   { get; set; }
        public string   DoctorId    { get; set; }
        public DateTime Date        { get; set; }
        public string   Diagnosis   { get; set; }
        public string   Procedure   { get; set; }
        public string   IcdCode     { get; set; }
        public double   Cost        { get; set; }
        public string   Notes       { get; set; }

        public TreatmentRecord(string id, string patientId, string doctorId,
                               DateTime date, string diagnosis, string procedure,
                               string icdCode, double cost)
        {
            Id        = id;
            PatientId = patientId;
            DoctorId  = doctorId;
            Date      = date;
            Diagnosis = diagnosis;
            Procedure = procedure;
            IcdCode   = icdCode;
            Cost      = cost;
            Notes     = string.Empty;
        }

        public override string ToString() =>
            $"[{Id}] {Date:dd.MM.yyyy} | {Diagnosis} | {Procedure} | ${Cost:F2}";
    }

    // ─── PatientFile (used by Memento) ────────────────────────────────────────

    public class PatientFile
    {
        public string                 PatientId        { get; private set; }
        public string                 PatientName      { get; set; }
        public string                 Allergies        { get; set; }
        public string                 ChronicConditions { get; set; }
        public List<TreatmentRecord>  TreatmentHistory { get; set; }
        public string                 LastModifiedBy   { get; set; }
        public DateTime               LastModifiedAt   { get; set; }

        public PatientFile(string patientId, string patientName)
        {
            PatientId        = patientId;
            PatientName      = patientName;
            Allergies        = "Niciuna";
            ChronicConditions = "Niciuna";
            TreatmentHistory = new List<TreatmentRecord>();
            LastModifiedBy   = "system";
            LastModifiedAt   = DateTime.Now;
        }

        public void AddTreatment(TreatmentRecord record)
        {
            TreatmentHistory.Add(record);
            LastModifiedAt = DateTime.Now;
        }

        public override string ToString()
        {
            return $"Dosar pacient: {PatientName} ({PatientId})\n" +
                   $"  Alergii: {Allergies}\n" +
                   $"  Afecțiuni cronice: {ChronicConditions}\n" +
                   $"  Tratamente înregistrate: {TreatmentHistory.Count}\n" +
                   $"  Ultima modificare: {LastModifiedBy} la {LastModifiedAt:dd.MM.yyyy HH:mm}";
        }
    }
}
