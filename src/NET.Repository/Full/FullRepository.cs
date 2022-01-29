using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using NET.Repository.Full.Logic;
using NET.Repository.Full.Utility;

namespace NET.Repository.Full
{
    public class FullRepository<T> : IFullRepository<T>
        where T : class
    {
        /// <summary>
        ///     Internal DbContext instance for changes manipulation
        /// </summary>
        private readonly DbContext _context;
        /// <summary>
        ///     Internal DbSet instance for data manipulation
        /// </summary>
        private readonly DbSet<T> _set;

        public FullRepository(DbContext context, DbSet<T> set)
        {
            _context = context;
            _set = set;
        }

        public IEnumerable<T> Get(Specification<T> condition = default,
            Expression<Func<T, object>>[] includeProperties = default,
            PaginationContext pageContext = default,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = default)
        {
            IQueryable<T> query = _set;

            if (condition != default)
            {
                query = query.Where(condition.ToExpression());
            }

            if (includeProperties != default)
            {
                query = includeProperties.Aggregate(
                    query,
                    (current, includeProperty) => current.Include(includeProperty));
            }

            if (pageContext != default)
            {
                query = query.Skip(pageContext.PageSize * (pageContext.PageNumber - 1))
                    .Take(pageContext.PageSize);
            }

            if (orderBy != default)
            {
                query = orderBy.Invoke(query).AsQueryable();
            }

            return query.ToList();
        }

        public T Find(Specification<T> condition,
            Expression<Func<T, object>>[] includeProperties)
        {
            var entities = Get(condition, includeProperties);

            return entities.FirstOrDefault();
        }

        public void Insert(T entity)
        {
            _set.Add(entity);
        }

        public void Insert(IEnumerable<T> entitiesToInsert)
        {
            foreach (var entity in entitiesToInsert)
            {
                Insert(entity);
            }
        }

        public void Delete(object id)
        {
            T entityToDelete = _set.Find(id);
            Delete(entityToDelete);
        }

        public void Delete(T entityToDelete)
        {
            if (_context.Entry(entityToDelete).State == EntityState.Detached)
            {
                _set.Attach(entityToDelete);
            }
            _set.Remove(entityToDelete);
        }

        public void Delete(IEnumerable<T> entitiesToDelete)
        {
            foreach (var entity in entitiesToDelete)
            {
                Delete(entity);
            }
        }

        public void Update(T entityToUpdate)
        {
            _set.Attach(entityToUpdate);
            _context.Entry(entityToUpdate).State = EntityState.Modified;
        }

        public void Update(IEnumerable<T> entitiesToUpdate)
        {
            foreach (var entity in entitiesToUpdate)
            {
                Update(entity);
            }
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _context.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}