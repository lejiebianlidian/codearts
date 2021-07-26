﻿#if NETSTANDARD2_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
#else
using System.Data.Entity;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// Represents the default implementation of the <see cref="IDbTransaction"/> interface.
    /// </summary>
    public class DbTransaction : IDbTransaction
    {
        private bool disposed = false;
        private readonly DbContext[] dbContexts;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTransaction"/> class.
        /// </summary>
        /// <param name="repositories">The contexts.</param>
        /// <exception cref="ArgumentNullException">The parameter <paramref name="repositories"/> is null</exception>
        /// <exception cref="ArgumentException">The parameter <paramref name="repositories"/> contains the duplicate value</exception>
        public DbTransaction(params ILinqRepository[] repositories)
        {
            if (IsRepeat(repositories ?? throw new ArgumentNullException(nameof(repositories))))
            {
                throw new ArgumentException(nameof(repositories));
            }

            if (repositories.Length > 1)
            {
                dbContexts = Distinct(repositories.Select(x => x.DBContext), repositories.Length);
            }
            else
            {
                dbContexts = repositories.Select(x => x.DBContext).ToArray();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTransaction"/> class.
        /// </summary>
        /// <param name="dbContexts">The contexts.</param>
        /// <exception cref="ArgumentNullException">The parameter <paramref name="dbContexts"/> is null</exception>
        /// <exception cref="ArgumentException">The parameter <paramref name="dbContexts"/> contains the duplicate value</exception>
        public DbTransaction(params DbContext[] dbContexts)
        {
            if (IsRepeat(dbContexts ?? throw new ArgumentNullException(nameof(dbContexts))))
            {
                throw new ArgumentException(nameof(dbContexts));
            }

            this.dbContexts = dbContexts;
        }

        /// <summary>
        /// 析构函数（自动调用<see cref="Dispose()"/>方法。）。
        /// </summary>
        ~DbTransaction() => Dispose();

        private static bool IsRepeat(ILinqRepository[] repositories)
        {
            if (repositories.Length == 1)
            {
                return false;
            }

            var list = new List<ILinqRepository>(repositories.Length);

            foreach (var repository in repositories)
            {
                if (repository is null)
                {
                    throw new ArgumentException();
                }

                if (list.Contains(repository))
                {
                    list.Clear();

                    return true;
                }

                list.Add(repository);
            }

            list.Clear();

            return false;
        }

        private static bool IsRepeat(DbContext[] dbContexts)
        {
            if (dbContexts.Length == 1)
            {
                return false;
            }

            var list = new List<DbContext>(dbContexts.Length);

            foreach (var context in dbContexts)
            {
                if (context is null)
                {
                    throw new ArgumentException();
                }

                if (list.Contains(context))
                {
                    list.Clear();

                    return true;
                }

                list.Add(context);
            }

            list.Clear();

            return false;
        }

        private static DbContext[] Distinct(IEnumerable<DbContext> dbContexts, int capacity)
        {
            var contexts = new List<DbContext>(capacity);

            foreach (var context in dbContexts)
            {
                if (context is null)
                {
                    throw new ArgumentException();
                }

                if (contexts.Contains(context))
                {
                    continue;
                }

                contexts.Add(context);
            }

            return contexts.ToArray();
        }

#if NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public int Commit(bool acceptAllChangesOnSuccess = false)
#else
        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public int Commit()
#endif
        {
            int count = 0;
#if NETSTANDARD2_0_OR_GREATER
            var list = new List<IDbContextTransaction>();
#else
            var list = new List<DbContextTransaction>();
#endif
            try
            {
                foreach (var context in dbContexts)
                {
                    list.Add(context.Database.BeginTransaction());

#if NETSTANDARD2_0_OR_GREATER
                    count += context.SaveChanges(acceptAllChangesOnSuccess);
#else
                    count += context.SaveChanges();
#endif
                }

                foreach (var item in list)
                {
                    item.Commit();
                }
            }
            catch
            {
                foreach (var item in list)
                {
                    item.Rollback();
                }

                throw;
            }
            finally
            {
                foreach (var item in list)
                {
                    item.Dispose();
                }
            }
            return count;
        }

#if NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(bool acceptAllChangesOnSuccess = false, CancellationToken cancellationToken = default)
        {
            int count = 0;
            var list = new List<IDbContextTransaction>();
            try
            {
                foreach (var context in dbContexts)
                {
                    list.Add(await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));

                    count += await context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
                }

                foreach (var item in list)
                {
                    await item.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                foreach (var item in list)
                {
                    await item.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }

                throw;
            }
            finally
            {
                foreach (var item in list)
                {
                    await item.DisposeAsync().ConfigureAwait(false);
                }
            }
            return count;
        }
#endif

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            int count = 0;
#if NETSTANDARD2_0_OR_GREATER
            var list = new List<IDbContextTransaction>();
#else
            var list = new List<DbContextTransaction>();
#endif
            try
            {
                foreach (var context in dbContexts)
                {
#if NETSTANDARD2_0_OR_GREATER
                    list.Add(await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));
#else
                    list.Add(context.Database.BeginTransaction());
#endif

                    count += await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var item in list)
                {
#if NETSTANDARD2_0_OR_GREATER
                    await item.CommitAsync(cancellationToken).ConfigureAwait(false);
#else
                    item.Commit();
#endif
                }
            }
            catch
            {
                foreach (var item in list)
                {
#if NETSTANDARD2_0_OR_GREATER
                    await item.RollbackAsync(cancellationToken).ConfigureAwait(false);
#else
                    item.Rollback();
#endif
                }

                throw;
            }
            finally
            {
                foreach (var item in list)
                {
#if NETSTANDARD2_0_OR_GREATER
                    await item.DisposeAsync().ConfigureAwait(false);
#else
                    item.Dispose();
#endif
                }
            }
            return count;
        }
#endif

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    foreach (var context in dbContexts)
                    {
                        var list = context.ChangeTracker
                            .Entries()
                            .ToList();

                        foreach (var entry in list)
                        {
                            entry.State = EntityState.Detached;
                        }
                    }

                    // dispose the db context.
                    Array.Clear(dbContexts, 0, dbContexts.Length);

                    disposed = true;
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}