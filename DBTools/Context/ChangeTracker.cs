using System;
using System.Collections.Generic;
using System.Linq;

namespace DBTools.Context
{
    /// <summary>
    /// Tracks entity state changes for batch persistence via SaveChanges.
    /// Opt-in change tracking: entities are only tracked when explicitly
    /// added via Add/Update/Remove or Attach.
    /// </summary>
    public class ChangeTracker
    {
        private readonly List<ChangeTrackerEntry> _entries = new List<ChangeTrackerEntry>();

        /// <summary>
        /// Gets all tracked entries.
        /// </summary>
        public IReadOnlyList<ChangeTrackerEntry> Entries => _entries.AsReadOnly();


        /// <summary>
        /// Tracks an entity with the specified state.
        /// If already tracked, updates the state.
        /// </summary>
        public void Track(object entity, EntityState state)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var existing = _entries.FirstOrDefault(e => ReferenceEquals(e.Entity, entity));
            if (existing != null)
            {
                existing.State = state;
            }
            else
            {
                _entries.Add(new ChangeTrackerEntry
                {
                    Entity = entity,
                    EntityType = entity.GetType(),
                    State = state
                });
            }
        }


        /// <summary>
        /// Gets entries that need to be persisted (Added, Modified, Deleted).
        /// </summary>
        public List<ChangeTrackerEntry> GetPendingEntries()
        {
            return _entries
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();
        }

        /// <summary>
        /// Marks all pending entries as Unchanged after successful save.
        /// Removes deleted entries from tracking.
        /// </summary>
        public void AcceptChanges()
        {
            _entries.RemoveAll(e => e.State == EntityState.Deleted);
            foreach (var entry in _entries)
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    entry.State = EntityState.Unchanged;
            }
        }

        /// <summary>
        /// Stops tracking the specified entity.
        /// </summary>
        public void Detach(object entity)
        {
            _entries.RemoveAll(e => ReferenceEquals(e.Entity, entity));
        }

        /// <summary>
        /// Clears all tracked entries.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>
        /// Checks if an entity is being tracked.
        /// </summary>
        public bool IsTracked(object entity)
        {
            return _entries.Any(e => ReferenceEquals(e.Entity, entity));
        }
    }


    /// <summary>
    /// Represents a tracked entity and its current state.
    /// </summary>
    public class ChangeTrackerEntry
    {
        /// <summary>
        /// The tracked entity instance.
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// The CLR type of the entity.
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// The current state of the entity.
        /// </summary>
        public EntityState State { get; set; }
    }

    /// <summary>
    /// Represents the state of a tracked entity.
    /// </summary>
    public enum EntityState
    {
        /// <summary>Entity is not being tracked.</summary>
        Detached,
        /// <summary>Entity exists in the database and has not been modified.</summary>
        Unchanged,
        /// <summary>Entity is new and will be inserted on SaveChanges.</summary>
        Added,
        /// <summary>Entity has been modified and will be updated on SaveChanges.</summary>
        Modified,
        /// <summary>Entity will be deleted on SaveChanges.</summary>
        Deleted
    }
}
