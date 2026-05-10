using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DentalClinic.Models;

namespace DentalClinic.Iterator
{
    // ════════════════════════════════════════════════════════════════════════════
    //  ITERATOR – Parcurgerea Colecției de Programări
    //
    //  Problemă: colecția de programări poate fi traversată în mai multe moduri
    //  (cronologic, pe medic, pe status, pe pacient) fără a expune structura
    //  internă a colecției sau a duplica logica de traversare.
    //
    //  Soluție: interfață IAppointmentIterator + implementări concrete pentru
    //  fiecare mod de traversare. AppointmentCollection = Aggregate care
    //  produce iteratori la cerere.
    // ════════════════════════════════════════════════════════════════════════════

    // ─── Iterator interface ───────────────────────────────────────────────────

    public interface IAppointmentIterator
    {
        bool   HasNext();
        Appointment Next();
        void   Reset();
        int    TotalCount { get; }
    }

    // ─── Aggregate interface ──────────────────────────────────────────────────

    public interface IAppointmentAggregate
    {
        IAppointmentIterator CreateChronologicalIterator();
        IAppointmentIterator CreateByDoctorIterator(string doctorId);
        IAppointmentIterator CreateByStatusIterator(AppointmentStatus status);
        IAppointmentIterator CreateByDateIterator(DateTime date);
        IAppointmentIterator CreateByPatientIterator(string patientId);
    }

    // ─── Concrete Iterators ───────────────────────────────────────────────────

    /// <summary>Parcurge programările în ordine cronologică.</summary>
    public class ChronologicalIterator : IAppointmentIterator
    {
        private readonly List<Appointment> _sorted;
        private int _index;

        public int TotalCount => _sorted.Count;

        public ChronologicalIterator(IEnumerable<Appointment> appointments)
        {
            _sorted = appointments.OrderBy(a => a.DateTime).ToList();
            _index  = 0;
        }

        public bool HasNext()       => _index < _sorted.Count;
        public Appointment Next()   => HasNext() ? _sorted[_index++] : null;
        public void Reset()         => _index = 0;
    }

    /// <summary>Parcurge programările unui medic specific, sortate cronologic.</summary>
    public class ByDoctorIterator : IAppointmentIterator
    {
        private readonly List<Appointment> _filtered;
        private int _index;

        public int TotalCount => _filtered.Count;

        public ByDoctorIterator(IEnumerable<Appointment> appointments, string doctorId)
        {
            _filtered = appointments
                .Where(a => a.Doctor.Id == doctorId)
                .OrderBy(a => a.DateTime)
                .ToList();
            _index = 0;
        }

        public bool HasNext()       => _index < _filtered.Count;
        public Appointment Next()   => HasNext() ? _filtered[_index++] : null;
        public void Reset()         => _index = 0;
    }

    /// <summary>Parcurge programările cu un anumit status.</summary>
    public class ByStatusIterator : IAppointmentIterator
    {
        private readonly List<Appointment> _filtered;
        private int _index;

        public int TotalCount => _filtered.Count;

        public ByStatusIterator(IEnumerable<Appointment> appointments, AppointmentStatus status)
        {
            _filtered = appointments
                .Where(a => a.Status == status)
                .OrderBy(a => a.DateTime)
                .ToList();
            _index = 0;
        }

        public bool HasNext()       => _index < _filtered.Count;
        public Appointment Next()   => HasNext() ? _filtered[_index++] : null;
        public void Reset()         => _index = 0;
    }

    /// <summary>Parcurge programările dintr-o zi calendaristică.</summary>
    public class ByDateIterator : IAppointmentIterator
    {
        private readonly List<Appointment> _filtered;
        private int _index;

        public int TotalCount => _filtered.Count;

        public ByDateIterator(IEnumerable<Appointment> appointments, DateTime date)
        {
            _filtered = appointments
                .Where(a => a.DateTime.Date == date.Date)
                .OrderBy(a => a.DateTime)
                .ToList();
            _index = 0;
        }

        public bool HasNext()       => _index < _filtered.Count;
        public Appointment Next()   => HasNext() ? _filtered[_index++] : null;
        public void Reset()         => _index = 0;
    }

    /// <summary>Parcurge istoricul programărilor unui pacient.</summary>
    public class ByPatientIterator : IAppointmentIterator
    {
        private readonly List<Appointment> _filtered;
        private int _index;

        public int TotalCount => _filtered.Count;

        public ByPatientIterator(IEnumerable<Appointment> appointments, string patientId)
        {
            _filtered = appointments
                .Where(a => a.Patient.Id == patientId)
                .OrderByDescending(a => a.DateTime)   // cel mai recent primul
                .ToList();
            _index = 0;
        }

        public bool HasNext()       => _index < _filtered.Count;
        public Appointment Next()   => HasNext() ? _filtered[_index++] : null;
        public void Reset()         => _index = 0;
    }

    // ─── Aggregate: AppointmentCollection ────────────────────────────────────

    /// <summary>
    /// Colecția de programări care implementează IEnumerable (compatibil C# foreach)
    /// și produce iteratori specializați fără a expune structura internă.
    /// </summary>
    public class AppointmentCollection : IAppointmentAggregate, IEnumerable<Appointment>
    {
        private readonly List<Appointment> _appointments = new();

        // ── Collection management ────────────────────────────────────────────

        public void Add(Appointment appt)    => _appointments.Add(appt);
        public void Remove(string id)        => _appointments.RemoveAll(a => a.Id == id);
        public int  Count                    => _appointments.Count;

        public Appointment GetById(string id) =>
            _appointments.FirstOrDefault(a => a.Id == id);

        // ── Iterator factory methods ─────────────────────────────────────────

        public IAppointmentIterator CreateChronologicalIterator() =>
            new ChronologicalIterator(_appointments);

        public IAppointmentIterator CreateByDoctorIterator(string doctorId) =>
            new ByDoctorIterator(_appointments, doctorId);

        public IAppointmentIterator CreateByStatusIterator(AppointmentStatus status) =>
            new ByStatusIterator(_appointments, status);

        public IAppointmentIterator CreateByDateIterator(DateTime date) =>
            new ByDateIterator(_appointments, date);

        public IAppointmentIterator CreateByPatientIterator(string patientId) =>
            new ByPatientIterator(_appointments, patientId);

        // ── IEnumerable<Appointment> (C# foreach support) ───────────────────

        public IEnumerator<Appointment> GetEnumerator() =>
            _appointments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        // ── Utility: print all via iterator ──────────────────────────────────

        public static void PrintAll(IAppointmentIterator iterator, string title)
        {
            Console.WriteLine($"  ── {title} ({iterator.TotalCount} înregistrări) ──");
            iterator.Reset();
            int i = 1;
            while (iterator.HasNext())
            {
                var appt = iterator.Next();
                Console.WriteLine($"    [{i++:D2}] {appt.DateTime:dd.MM.yyyy HH:mm} | {appt.Patient.Name,-20} | " +
                                  $"{appt.Doctor.Name,-20} | {appt.Service,-25} | {appt.Status}");
            }
            if (iterator.TotalCount == 0)
                Console.WriteLine("    (nicio programare)");
        }
    }
}
