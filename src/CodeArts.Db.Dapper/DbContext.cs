﻿using CodeArts.Db.Exceptions;
using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Dapper
{
    /// <summary>
    /// 数据上下文。
    /// </summary>
    public class DbContext : IDisposable
    {
        private readonly IReadOnlyConnectionConfig connectionConfig;
        private readonly List<IDbConnection> connections = new List<IDbConnection>();
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();

        /// <summary>
        /// inheritdoc
        /// </summary>
        protected DbContext()
        {
            connectionConfig = GetDbConfig() ?? throw new DException("未找到数据库链接!");
        }

        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext(IReadOnlyConnectionConfig connectionConfig)
        {
            this.connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }

        /// <summary>
        /// 获取数据库配置。
        /// </summary>
        /// <returns></returns>
        protected virtual IReadOnlyConnectionConfig GetDbConfig()
        {
            var attr = DbConfigCache.GetOrAdd(GetType(), type =>
            {
                return (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute), true);
            });

            return attr?.GetConfig() ?? throw new DException("未找到数据库链接!");
        }

        /// <summary> 连接名称。 </summary>
        public string Name => connectionConfig.Name;

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName => connectionConfig.ProviderName;

        private IDbConnectionAdapter connectionAdapter;
        private bool disposedValue;

        /// <summary>
        /// 适配器。
        /// </summary>
        protected IDbConnectionAdapter DbAdapter
        {
            get
            {
                if (connectionAdapter is null || !string.Equals(connectionAdapter.ProviderName, connectionConfig.ProviderName))
                {
                    connectionAdapter = CreateDbAdapter(connectionConfig.ProviderName);
                }

                return connectionAdapter;
            }
        }

        /// <summary>
        /// 创建适配器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbConnectionAdapter CreateDbAdapter(string providerName) => DapperConnectionManager.Get(providerName);

        /// <summary>
        /// 创建数据库查询器。
        /// </summary>
        /// <param name="useCache">优先复用链接池，否则：始终创建新链接。</param>
        /// <returns></returns>
        protected virtual IDbConnection CreateDb(bool useCache = true)
        {
            var connection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, DbAdapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, DbAdapter, useCache);

            connections.Add(connection);

            return connection;
        }

        /// <summary>
        /// 转分页SQL（listSql;countSql;）。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="pageIndex">页面索引。</param>
        /// <param name="pageSize">分页条数。</param>
        /// <returns></returns>
        protected virtual string ToPagedSql(SQL sql, int pageIndex, int pageSize)
        {
            string mainSql = Format(sql.ToSQL(pageIndex, pageSize));

            if (DbAdapter.Settings.Engine == DatabaseEngine.MySQL)
            {
                int i = 6;/* 跳过 SELECT 的计算 */

                for (int length = mainSql.Length; i < length; i++)
                {
                    char c = mainSql[i];

                    if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z'))
                    {
                        break;
                    }
                }

                var sb = new StringBuilder();

                return sb.Append(mainSql, 0, i)
                     .Append(' ')
                     .Append("SQL_CALC_FOUND_ROWS")
                     .Append(mainSql, i, mainSql.Length - i)
                     .Append(';')
                     .Append("SELECT FOUND_ROWS()")
                     .ToString();
            }

            string countSql = Format(sql.ToCountSQL());

            return string.Concat(mainSql, ";", countSql);
        }

        /// <summary>
        /// 将SQL转化为当前数据库规范的语句。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        protected virtual string Format(SQL sql)
        {
            if (sql is null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            return sql.ToString(DbAdapter.Settings);
        }

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QuerySingle<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QuerySingle<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QuerySingleOrDefault<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QuerySingleOrDefault<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QueryFirst<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QueryFirst<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QueryFirstOrDefault<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QueryFirstOrDefault<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询分页数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="pageIndex">页面，索引从“0”开始。</param>
        /// <param name="pageSize">一页显示多少条。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public PagedList<T> Query<T>(SQL sql, int pageIndex, int pageSize, object param = null, int? commandTimeout = null)
        {
            var sqlStr = ToPagedSql(sql, pageIndex, pageSize);

            using (IDbConnection connection = CreateDb())
            {
                using (var reader = connection.QueryMultiple(sqlStr, param, commandTimeout: commandTimeout))
                {
                    var results = reader.Read<T>();

                    int count = reader.ReadSingle<int>();

                    return new PagedList<T>(results as List<T> ?? results.ToList(), pageIndex, pageSize, count);
                }
            }
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QuerySingleAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QuerySingleAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QuerySingleOrDefaultAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QueryFirstAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QueryFirstAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QueryFirstOrDefaultAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询分页数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="pageIndex">页面，索引从“0”开始。</param>
        /// <param name="pageSize">一页显示多少条。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<PagedList<T>> QueryAsync<T>(SQL sql, int pageIndex, int pageSize, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = ToPagedSql(sql, pageIndex, pageSize);

            using (IDbConnection connection = CreateDb())
            {
                using (var reader = await connection.QueryMultipleAsync(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken)).ConfigureAwait(false))
                {
                    var results = await reader.ReadAsync<T>().ConfigureAwait(false);

                    int count = await reader.ReadSingleAsync<int>().ConfigureAwait(false);

                    return new PagedList<T>(results as List<T> ?? results.ToList(), pageIndex, pageSize, count);
                }
            }
        }
#endif

        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public int Execute(SQL sql, object param = null, int? commandTimeout = null)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.Execute(sqlStr, param, null, commandTimeout);
            }
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.ExecuteAsync(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }
#endif

        /// <summary>
        /// 释放。
        /// </summary>
        /// <param name="disposing">释放。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    foreach (var connection in connections)
                    {
                        connection.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        /// ~DbContext()
        /// {
        ///     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        ///     Dispose(disposing: false);
        /// }
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }
    }
}
