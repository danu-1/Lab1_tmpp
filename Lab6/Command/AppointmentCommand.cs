using System;
using System.Collections.Generic;
using DentalClinic.Models;

namespace DentalClinic.Command
{
    // ════════════════════════════════════════════════════════════════════════════
    //  COMMAND – Gestionare Programări cu Undo / Redo
    //
    //  Problemă: operațiile asupra programărilor (creare, anulare, reprogramare)
    //  trebuie să poată fi anulate (recepționista a greșit data) sau re-aplicate.
    //  Cu metode directe, undo-ul ar necesita logică specială în fiecare loc.
    //
    //  Soluție: fiecare operație devine un obiect Command cu Execute() + Undo().
    //  CommandInvoker ține stiva history/redo și orchestrează undo/redo.
    // ════════════════════════════════════════════════════════════════════════════

    // ─── Command interface ────────────────────────────────────────────────────

    public interface ICommand
    {
        string CommandName { get; }
        void Execute();
        void Undo();
        string GetDescription();
    }

    // ─── Receiver: AppointmentRepository ─────────────────────────────────────

    /// <summary>
    /// Receptorul real care stochează și modifică programările.
    /// Commands apelează metodele acestuia; nu știu de Invoker sau alte comenzi.
    /// </summary>
    public class AppointmentRepository
    {
        private readonly Dictionary<string, Appointment> _store = new();

        public void Add(Appointment appt)
        {
            _store[appt.Id] = appt;
            Console.WriteLine($"    [Repo] ✅ Adăugat: {appt}");
        }

        public void Remove(string id)
        {
            if (_store.TryGetValue(id, out var appt))
            {
                _store.Remove(id);
                Console.WriteLine($"    [Repo] 🗑  Eliminat: [{id}] {appt.Patient.Name}");
            }
        }

        public void Update(Appointment appt)
        {
            _store[appt.Id] = appt;
            Console.WriteLine($"    [Repo] 🔄 Actualizat: {appt}");
        }

        public Appointment Get(string id)
            => _store.TryGetValue(id, out var a) ? a : null;

        public bool Exists(string id) => _store.ContainsKey(id);

        public IEnumerable<Appointment> GetAll() => _store.Values;
    }

    // ─── Concrete Commands ────────────────────────────────────────────────────

    /// <summary>Crează o programare nouă. Undo → șterge programarea creată.</summary>
    public class CreateAppointmentCommand : ICommand
    {
        private readonly AppointmentRepository _repo;
        private readonly Appointment           _appointment;

        public string CommandName => "CreateAppointment";

        public CreateAppointmentCommand(AppointmentRepository repo, Appointment appointment)
        {
            _repo        = repo;
            _appointment = appointment;
        }

        public void Execute()
        {
            Console.WriteLine($"    [CMD] ▶ Creare programare {_appointment.Id}...");
            _repo.Add(_appointment);
        }

        public void Undo()
        {
            Console.WriteLine($"    [CMD] ↩ UNDO creare → eliminare {_appointment.Id}");
            _repo.Remove(_appointment.Id);
        }

        public string GetDescription() =>
            $"Creare programare [{_appointment.Id}]: {_appointment.Patient.Name} " +
            $"– {_appointment.Service} – {_appointment.DateTime:dd.MM.yyyy HH:mm}";
    }

    /// <summary>Anulează o programare existentă. Undo → restaurează starea anterioară.</summary>
    public class CancelAppointmentCommand : ICommand
    {
        private readonly AppointmentRepository _repo;
        private readonly string                _appointmentId;
        private readonly string                _reason;
        private Appointment                    _previousState; // salvat pentru Undo

        public string CommandName => "CancelAppointment";

        public CancelAppointmentCommand(AppointmentRepository repo,
                                        string appointmentId,
                                        string reason = "Anulare la cererea pacientului")
        {
            _repo          = repo;
            _appointmentId = appointmentId;
            _reason        = reason;
        }

        public void Execute()
        {
            var appt = _repo.Get(_appointmentId);
            if (appt == null) { Console.WriteLine($"    [CMD] ⚠ Programarea {_appointmentId} nu există."); return; }

            _previousState = appt.Clone(); // snapshot pentru Undo
            appt.Status = AppointmentStatus.Cancelled;
            appt.Notes  = _reason;
            _repo.Update(appt);
            Console.WriteLine($"    [CMD] ▶ Anulare programare {_appointmentId}. Motiv: {_reason}");
        }

        public void Undo()
        {
            if (_previousState == null) return;
            Console.WriteLine($"    [CMD] ↩ UNDO anulare → restaurare {_appointmentId}");
            _repo.Update(_previousState);
        }

        public string GetDescription() => $"Anulare programare [{_appointmentId}]: {_reason}";
    }

    /// <summary>Reprogramează la o nouă dată. Undo → revine la data originală.</summary>
    public class RescheduleAppointmentCommand : ICommand
    {
        private readonly AppointmentRepository _repo;
        private readonly string                _appointmentId;
        private readonly DateTime              _newDateTime;
        private DateTime                       _originalDateTime;

        public string CommandName => "RescheduleAppointment";

        public RescheduleAppointmentCommand(AppointmentRepository repo,
                                            string appointmentId,
                                            DateTime newDateTime)
        {
            _repo          = repo;
            _appointmentId = appointmentId;
            _newDateTime   = newDateTime;
        }

        public void Execute()
        {
            var appt = _repo.Get(_appointmentId);
            if (appt == null) { Console.WriteLine($"    [CMD] ⚠ Programarea {_appointmentId} nu există."); return; }

            _originalDateTime = appt.DateTime;
            appt.DateTime = _newDateTime;
            appt.Status   = AppointmentStatus.Rescheduled;
            _repo.Update(appt);
            Console.WriteLine($"    [CMD] ▶ Reprogramare {_appointmentId}: {_originalDateTime:dd.MM.yyyy HH:mm} → {_newDateTime:dd.MM.yyyy HH:mm}");
        }

        public void Undo()
        {
            var appt = _repo.Get(_appointmentId);
            if (appt == null) return;
            Console.WriteLine($"    [CMD] ↩ UNDO reprogramare → {_originalDateTime:dd.MM.yyyy HH:mm}");
            appt.DateTime = _originalDateTime;
            appt.Status   = AppointmentStatus.Scheduled;
            _repo.Update(appt);
        }

        public string GetDescription() =>
            $"Reprogramare [{_appointmentId}] → {_newDateTime:dd.MM.yyyy HH:mm}";
    }

    /// <summary>Modifică prețul unui serviciu. Undo → revine la prețul anterior.</summary>
    public class UpdatePriceCommand : ICommand
    {
        private readonly AppointmentRepository _repo;
        private readonly string                _appointmentId;
        private readonly double                _newPrice;
        private double                         _originalPrice;

        public string CommandName => "UpdatePrice";

        public UpdatePriceCommand(AppointmentRepository repo, string appointmentId, double newPrice)
        {
            _repo          = repo;
            _appointmentId = appointmentId;
            _newPrice      = newPrice;
        }

        public void Execute()
        {
            var appt = _repo.Get(_appointmentId);
            if (appt == null) return;
            _originalPrice = appt.BasePrice;
            appt.BasePrice = _newPrice;
            _repo.Update(appt);
            Console.WriteLine($"    [CMD] ▶ Preț actualizat {_appointmentId}: ${_originalPrice:F2} → ${_newPrice:F2}");
        }

        public void Undo()
        {
            var appt = _repo.Get(_appointmentId);
            if (appt == null) return;
            Console.WriteLine($"    [CMD] ↩ UNDO preț → ${_originalPrice:F2}");
            appt.BasePrice = _originalPrice;
            _repo.Update(appt);
        }

        public string GetDescription() => $"Actualizare preț [{_appointmentId}] → ${_newPrice:F2}";
    }

    // ─── MacroCommand (composite) ─────────────────────────────────────────────

    /// <summary>
    /// Grupează mai multe comenzi într-una singură.
    /// Execute le rulează în ordine, Undo le anulează în ordine inversă.
    /// </summary>
    public class MacroCommand : ICommand
    {
        private readonly List<ICommand> _commands;
        private readonly string        _name;

        public string CommandName => _name;

        public MacroCommand(string name, params ICommand[] commands)
        {
            _name     = name;
            _commands = new List<ICommand>(commands);
        }

        public void Execute()
        {
            Console.WriteLine($"    [MACRO] ▶ Execuție macro '{_name}' ({_commands.Count} comenzi):");
            foreach (var cmd in _commands)
                cmd.Execute();
        }

        public void Undo()
        {
            Console.WriteLine($"    [MACRO] ↩ UNDO macro '{_name}' (ordine inversă):");
            for (int i = _commands.Count - 1; i >= 0; i--)
                _commands[i].Undo();
        }

        public string GetDescription() => $"Macro '{_name}' [{_commands.Count} comenzi]";
    }

    // ─── Invoker: CommandInvoker ──────────────────────────────────────────────

    /// <summary>
    /// Invocatorul care gestionează execuția, istoricul și undo/redo.
    /// Clientul trimite comenzi spre Invoker – nu execută direct.
    /// </summary>
    public class CommandInvoker
    {
        private readonly Stack<ICommand> _history = new();
        private readonly Stack<ICommand> _redoStack = new();

        public int HistoryCount => _history.Count;
        public int RedoCount    => _redoStack.Count;

        public void Execute(ICommand command)
        {
            command.Execute();
            _history.Push(command);
            _redoStack.Clear(); // orice comandă nouă golește redo stack
        }

        public bool Undo()
        {
            if (_history.Count == 0)
            {
                Console.WriteLine("    [Invoker] ⚠ Nimic de anulat (history gol).");
                return false;
            }
            var cmd = _history.Pop();
            Console.WriteLine($"    [Invoker] Undo: '{cmd.CommandName}'");
            cmd.Undo();
            _redoStack.Push(cmd);
            return true;
        }

        public bool Redo()
        {
            if (_redoStack.Count == 0)
            {
                Console.WriteLine("    [Invoker] ⚠ Nimic de re-aplicat (redo gol).");
                return false;
            }
            var cmd = _redoStack.Pop();
            Console.WriteLine($"    [Invoker] Redo: '{cmd.CommandName}'");
            cmd.Execute();
            _history.Push(cmd);
            return true;
        }

        public void PrintHistory()
        {
            Console.WriteLine($"  ── Istoricul comenzilor ({_history.Count}) ──");
            int i = _history.Count;
            foreach (var cmd in _history)
                Console.WriteLine($"    [{i--}] {cmd.GetDescription()}");
        }
    }
}
