#if NETSTANDARD2_0_OR_GREATER
using CodeArts.Db;
using CodeArts.Db.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// EF 注入。
    /// </summary>
    public static class EntityFrameworkServiceCollectionExtentions
    {
        private static readonly Type DbContextType = typeof(DbContext);
        private static readonly Type IEntiyType = typeof(IEntiy);
        private static readonly Type IEntity_T1_TypeDefinition = typeof(IEntiy<>);
        private static readonly Type ILinqRepository_T1_TypeDefinition = typeof(ILinqRepository<>);
        private static readonly Type ILinqRepository_T1_T2_TypeDefinition = typeof(ILinqRepository<,>);
        private static readonly Type LinqRepository_T1_T2_TypeDefinition = typeof(LinqRepository<,>);
        private static readonly Type LinqRepository_T1_T2_T3_TypeDefinition = typeof(LinqRepository<,,>);

        private class LinqRepository<TContext, TEntity> : LinqRepository<TEntity>, ILinqRepository<TEntity>, ILinqRepository, IQueryable<TEntity>, IOrderedQueryable<TEntity>, IEnumerable<TEntity>, IQueryable, IOrderedQueryable, IEnumerable
            where TContext : DbContext
            where TEntity : class, IEntiy, new()
        {
            public LinqRepository(TContext context) : base(context)
            {
            }
        }

        private class LinqRepository<TContext, TEntity, TKey> : CodeArts.Db.EntityFramework.LinqRepository<TEntity, TKey>, ILinqRepository<TEntity, TKey>, ILinqRepository<TEntity>, ILinqRepository, IQueryable<TEntity>, IOrderedQueryable<TEntity>, IEnumerable<TEntity>, IQueryable, IOrderedQueryable, IEnumerable
            where TContext : DbContext
            where TEntity : class, IEntiy<TKey>, new()
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            public LinqRepository(TContext context) : base(context)
            {
            }
        }

        /// <summary>
        /// 注册上下文（并注册上下文中数据表的仓库支持）。
        /// </summary>
        /// <typeparam name="TContext">数据库上下文。</typeparam>
        /// <param name="services">服务集合。</param>
        /// <param name="lifetime">在容器中注册仓库和DbContext服务的生存期。</param>
        /// <param name="optionsLifetime">在容器中注册DbContextOptions服务的生存期。</param>
        /// <returns></returns>
        public static IServiceCollection AddDefaultRepositories<TContext>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            var contextType = typeof(TContext);

            var propertys = contextType.GetProperties();

            propertys
                .Where(x => x.PropertyType.IsGenericType && x.PropertyType.DeclaringType != DbContextType && x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ForEach(x =>
                {
                    var entityType = x.PropertyType.GetGenericArguments()[0];

                    if (!IEntiyType.IsAssignableFrom(entityType))
                    {
                        return;
                    }

                    bool flag = false;
                    Type keyType = null;
                    var targetType = entityType;

                    do
                    {
                        var interfaces = targetType.GetInterfaces();

                        if (interfaces.Length > 0)
                        {
                            foreach (var interfaceType in interfaces)
                            {
                                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == IEntity_T1_TypeDefinition)
                                {
                                    keyType = interfaceType.GetGenericArguments()[0];

                                    flag = true;

                                    break;
                                }
                            }
                        }

                        if (flag)
                        {
                            break;
                        }

                        targetType = targetType.BaseType;

                    } while (!(targetType is null || targetType == typeof(object)));

                    if (flag)
                    {
                        services.Add(new ServiceDescriptor(
                            ILinqRepository_T1_T2_TypeDefinition.MakeGenericType(entityType, keyType),
                            LinqRepository_T1_T2_T3_TypeDefinition.MakeGenericType(contextType, entityType, keyType),
                            lifetime));
                    }

                    services.Add(new ServiceDescriptor(
                        ILinqRepository_T1_TypeDefinition.MakeGenericType(entityType),
                        LinqRepository_T1_T2_TypeDefinition.MakeGenericType(contextType, entityType),
                        lifetime));
                });

            return services.AddDbContext<TContext>(lifetime, optionsLifetime);
        }
    }
}
#endif