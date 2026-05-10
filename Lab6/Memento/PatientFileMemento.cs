using System;
using System.Collections.Generic;
using System.Linq;
using DentalClinic.Models;

namespace DentalClinic.Memento
{
    // ════════════════════════════════════════════════════════════════════════════
    //  MEMENTO – Versioning Dosar Pacient (Save / Load)
    //
    //  Problemă: medicul poate greși când editează dosarul pacientului
    //  (date alergii, tratamente). Trebuie posibilitate de a reveni la o
    //  versiune anterioară fără a expune structura internă a dosarului.
    //
    //  Soluție:
    //    Originator  = PatientFileEditor  (obiectul cu stare)
    //    Memento     = PatientFileMemento  (snapshot opac)
    //    Caretaker   = PatientFileHistory  (gestionează stiva de versiuni)
    // ════════════════════════════════════════════════════════════════════════════

    // ─── Memento (snapshot opac) ───────────────────────────────────────────────

    public class PatientFileMemento
    {
        // Starea salvată – vizibilă doar din Originator (nested class)
        internal string     PatientName       { get; }
        internal string     Allergies         { get; }
        internal string     ChronicConditions { get; }
        internal List<TreatmentRecord> TreatmentHistory { get; }
        internal string     LastModifiedBy    { get; }
        internal DateTime   SavedAt           { get; }
        public   string     Label             { get; }

        internal PatientFileMemento(string patientName, string allergies,
                                    string chronicConditions,
                                    IEnumerable<TreatmentRecord> treatments,
                                    string lastModifiedBy, string label)
        {
            PatientName       = patientName;
            Allergies         = allergies;
            ChronicConditions = chronicConditions;
            TreatmentHistory  = new List<TreatmentRecord>(treatments);
            LastModifiedBy    = lastModifiedBy;
            SavedAt           = DateTime.Now;
            Label             = label;
        }

        public override string ToString() =>
            $"'{Label}' salvat la {SavedAt:dd.MM.yyyy HH:mm:ss} de {LastModifiedBy}";
    }

    // ─── Originator: PatientFileEditor ────────────────────────────────────────

    /// <summary>
    /// Originator: deține și modifică starea dosarului pacientului.
    /// Poate crea Memento (snapshot) și se poate restaura dintr-un Memento.
    /// Detaliile interne NU sunt expuse extern – Caretaker operează cu obiecte opace.
    /// </summary>
    public class PatientFileEditor
    {
        private PatientFile _file;

        public string PatientId   => _file.PatientId;
        public string PatientName
        {
            get => _file.PatientName;
            set { _file.PatientName = value; _file.LastModifiedAt = DateTime.Now; }
        }
        public string Allergies
        {
            get => _file.Allergies;
            set { _file.Allergies = value; _file.LastModifiedAt = DateTime.Now; }
        }
        public string ChronicConditions
        {
            get => _file.ChronicConditions;
            set { _file.ChronicConditions = value; _file.LastModifiedAt = DateTime.Now; }
        }
        public string LastModifiedBy
        {
            get => _file.LastModifiedBy;
            set { _file.LastModifiedBy = value; }
        }

        public PatientFileEditor(PatientFile file)
        {
            _file = file;
        }

        public void AddTreatment(TreatmentRecord record)
        {
            _file.AddTreatment(record);
            Console.WriteLine($"    [Editor] Tratament adăugat: {record.Procedure} ({record.Date:dd.MM.yyyy})");
        }

        public int TreatmentCount => _file.TreatmentHistory.Count;

        // ── Memento operations ───────────────────────────────────────────────

        /// <summary>Creează un snapshot al stării curente (Memento).</summary>
        public PatientFileMemento Save(string label = "")
        {
            string lbl = string.IsNullOrWhiteSpace(label)
                ? $"auto_{DateTime.Now:HHmmss}"
                : label;

            var memento = new PatientFileMemento(
                _file.PatientName,
                _file.Allergies,
                _file.ChronicConditions,
                _file.TreatmentHistory,
                _file.LastModifiedBy,
                lbl
            );
            Console.WriteLine($"    [Editor] 💾 Snapshot creat: {memento}");
            return memento;
        }

        /// <summary>Restaurează starea din Memento primit de la Caretaker.</summary>
        public void Restore(PatientFileMemento memento)
        {
            _file.PatientName       = memento.PatientName;
            _file.Allergies         = memento.Allergies;
            _file.ChronicConditions = memento.ChronicConditions;
            _file.TreatmentHistory  = new List<TreatmentRecord>(memento.TreatmentHistory);
            _file.LastModifiedBy    = memento.LastModifiedBy;
            _file.LastModifiedAt    = DateTime.Now;
            Console.WriteLine($"    [Editor] 🔄 Restaurat din snapshot: {memento}");
        }

        public override string ToString() => _file.ToString();
    }

    // ─── Caretaker: PatientFileHistory ────────────────────────────────────────

    /// <summary>
    /// Caretaker: gestionează stiva de versiuni (Memento-uri).
    /// NU accesează conținutul intern al Memento – doar îl stochează și returnează.
    /// Implementează Save / Undo / Redo complet.
    /// </summary>
    public class PatientFileHistory
    {
        private readonly Stack<PatientFileMemento> _undoStack = new();
        private readonly Stack<PatientFileMemento> _redoStack = new();
        private readonly PatientFileEditor         _editor;
        private readonly int                       _maxVersions;

        public int VersionCount  => _undoStack.Count;
        public int RedoCount     => _redoStack.Count;

        public PatientFileHistory(PatientFileEditor editor, int maxVersions = 20)
        {
            _editor      = editor;
            _maxVersions = maxVersions;
        }

        /// <summary>Salvează versiunea curentă și o adaugă în stivă.</summary>
        public void SaveVersion(string label = "")
        {
            var memento = _editor.Save(label);
            _undoStack.Push(memento);
            _redoStack.Clear();

            // Menține limita de versiuni
            if (_undoStack.Count > _maxVersions)
            {
                var temp = _undoStack.ToArray();
                _undoStack.Clear();
                foreach (var m in temp.Take(_maxVersions))
                    _undoStack.Push(m);
            }
        }

        /// <summary>Revine la versiunea anterioară.</summary>
        public bool Undo()
        {
            if (_undoStack.Count == 0)
            {
                Console.WriteLine("    [History] ⚠ Nicio versiune anterioară disponibilă.");
                return false;
            }
            // Salvăm starea curentă în redo înainte de restaurare
            var currentSnapshot = _editor.Save("_redo_temp");
            _redoStack.Push(currentSnapshot);

            var previous = _undoStack.Pop();
            Console.WriteLine($"    [History] ↩ Revenire la versiunea: {previous}");
            _editor.Restore(previous);
            return true;
        }

        /// <summary>Re-aplică versiunea anulată.</summary>
        public bool Redo()
        {
            if (_redoStack.Count == 0)
            {
                Console.WriteLine("    [History] ⚠ Nimic de re-aplicat.");
                return false;
            }
            var current  = _editor.Save("_undo_temp");
            _undoStack.Push(current);

            var next = _redoStack.Pop();
            Console.WriteLine($"    [History] ↪ Re-aplicare versiune: {next}");
            _editor.Restore(next);
            return true;
        }

        /// <summary>Afișează toate versiunile disponibile.</summary>
        public void PrintVersionList()
        {
            Console.WriteLine($"  ── Versiuni disponibile ({_undoStack.Count}) ──");
            int i = _undoStack.Count;
            foreach (var m in _undoStack)
                Console.WriteLine($"    [{i--}] {m}");

            if (_redoStack.Count > 0)
            {
                Console.WriteLine($"  ── Versiuni Redo ({_redoStack.Count}) ──");
                int j = 1;
                foreach (var m in _redoStack)
                    Console.WriteLine($"    [+{j++}] {m}");
            }
        }
    }
}
