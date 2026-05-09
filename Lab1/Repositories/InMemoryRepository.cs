using DentalClinic.Interfaces;

namespace DentalClinic.Repositories
{
    /// <summary>
    /// Implementare generică in-memory a repository-ului.
    /// SRP  – responsabilă exclusiv de stocarea și recuperarea datelor.
    /// DIP  – implementează interfața IRepository; serviciile depind de
    ///        abstracție, nu de această clasă concretă.
    /// OCP  – poate fi înlocuită cu o implementare DB fără a schimba serviciile.
    /// </summary>
    public class InMemoryRepository<T> : IRepository<T> where T : class, IIdentifiable
    {
        private readonly Dictionary<int, T> _store = new();

        public void Add(T entity)
        {
            if (_store.ContainsKey(entity.Id))
                throw new InvalidOperationException($"Entitatea cu Id={entity.Id} există deja.");
            _store[entity.Id] = entity;
        }

        public T? GetById(int id) =>
            _store.TryGetValue(id, out var entity) ? entity : null;

        public IEnumerable<T> GetAll() => _store.Values;

        public void Update(T entity)
        {
            if (!_store.ContainsKey(entity.Id))
                throw new KeyNotFoundException($"Entitatea cu Id={entity.Id} nu a fost găsită.");
            _store[entity.Id] = entity;
        }

        public void Delete(int id)
        {
            if (!_store.Remove(id))
                throw new KeyNotFoundException($"Entitatea cu Id={id} nu a fost găsită.");
        }
    }
}
