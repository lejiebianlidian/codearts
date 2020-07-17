﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据库(Close和Dispose接口虚拟，通过接口调用时不会真正关闭或释放，只有通过类调用才会真实的执行)
    /// </summary>
    public class DbConnection : IDbConnection
    {
        /// <summary>
        /// 数据读取器。
        /// </summary>
        private class DbReader : IDataReader
        {
            private readonly DbCommand command;
            private readonly IDataReader reader;

            public DbReader(DbCommand command, IDataReader reader)
            {
                this.command = command;
                this.reader = reader;
            }
            public object this[int i] => reader[i];

            public object this[string name] => reader[name];

            public int Depth => reader.Depth;

            public bool IsClosed => reader.IsClosed;

            public int RecordsAffected => reader.RecordsAffected;

            public int FieldCount => reader.FieldCount;

            public void Close()
            {
                reader.Close();
                command.Remove(this);
            }

            public void Dispose()
            {
                reader.Dispose();
                command.Remove(this);
            }

            public bool GetBoolean(int i) => reader.GetBoolean(i);

            public byte GetByte(int i) => reader.GetByte(i);

            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
            public char GetChar(int i) => reader.GetChar(i);

            public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

            public IDataReader GetData(int i) => reader.GetData(i);

            public string GetDataTypeName(int i) => reader.GetDataTypeName(i);

            public DateTime GetDateTime(int i) => reader.GetDateTime(i);

            public decimal GetDecimal(int i) => reader.GetDecimal(i);

            public double GetDouble(int i) => reader.GetDouble(i);

            public Type GetFieldType(int i) => reader.GetFieldType(i);

            public float GetFloat(int i) => reader.GetFloat(i);

            public Guid GetGuid(int i) => reader.GetGuid(i);

            public short GetInt16(int i) => reader.GetInt16(i);

            public int GetInt32(int i) => reader.GetInt32(i);

            public long GetInt64(int i) => reader.GetInt64(i);

            public string GetName(int i) => reader.GetName(i);

            public int GetOrdinal(string name) => reader.GetOrdinal(name);

            public DataTable GetSchemaTable() => reader.GetSchemaTable();

            public string GetString(int i) => reader.GetString(i);

            public object GetValue(int i) => reader.GetValue(i);

            public int GetValues(object[] values) => reader.GetValues(values);

            public bool IsDBNull(int i) => reader.IsDBNull(i);

            public bool NextResult() => reader.NextResult();

            public bool Read() => reader.Read();
        }

        /// <summary>
        /// 命令。
        /// </summary>
        private class DbCommand : IDbCommand
        {
            private bool isAlive = false;
            private volatile int commandType = 0;
            private readonly IDbCommand command;
            private readonly DbConnection connection;
            private readonly List<DbReader> dataReaders = new List<DbReader>();

            public DbCommand(DbConnection connection, IDbCommand command)
            {
                this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                this.command = command ?? throw new ArgumentNullException(nameof(command));
            }

            /// <summary>
            /// 连接。
            /// </summary>
            public IDbConnection Connection
            {
                get => command.Connection;
                set
                {
                    connection.Remove(this);

                    if (value is DbConnection db)
                    {
                        db.Add(this);
                    }

                    command.Connection = value;
                }
            }

            /// <summary>
            /// 事务。
            /// </summary>
            public IDbTransaction Transaction { get => command.Transaction; set => command.Transaction = value; }

            /// <summary>
            /// 命令。
            /// </summary>
            public string CommandText { get => command.CommandText; set => command.CommandText = value; }

            /// <summary>
            /// 命令超时时间。
            /// </summary>
            public int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }

            /// <summary>
            /// 命令类型。
            /// </summary>
            public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }

            /// <summary>
            /// 参数。
            /// </summary>
            public IDataParameterCollection Parameters => command.Parameters;
            /// <summary>
            /// 获取或设置命令结果在由 System.Data.Common.DbDataAdapter 的 System.Data.IDataAdapter.Update(System.Data.DataSet) 方法使用时应用于 System.Data.DataRow 的方式。
            /// </summary>
            public UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }

            /// <summary>
            /// 取消指令。
            /// </summary>
            public void Cancel() => command.Cancel();

            /// <summary>
            /// 创建参数。
            /// </summary>
            /// <returns></returns>
            public IDbDataParameter CreateParameter() => command.CreateParameter();

            /// <summary>
            /// 释放资源。
            /// </summary>
            public void Dispose()
            {
                dataReaders.Clear();

                connection.Remove(this);

                command.Dispose();
            }

            public IDataReader Add(IDataReader reader)
            {
                commandType = 2;

                dataReaders.Add(new DbReader(this, reader));

                return reader;
            }

            public void Remove(DbReader reader)
            {
                dataReaders.Remove(reader);
            }

            /// <summary>
            /// 执行，返回影响行。
            /// </summary>
            /// <returns></returns>
            public int ExecuteNonQuery()
            {
                if (commandType == 0)
                {
                    commandType = 1;
                }

                try
                {
                    return command.ExecuteNonQuery();
                }
                finally
                {
                    if (commandType == 1)
                    {
                        commandType = 0;
                    }
                }
            }

            /// <summary>
            /// 存活的。
            /// </summary>
            public bool IsAlive => isAlive || commandType == 0 || dataReaders.Count > 0;

            /// <summary>
            /// 执行并生成读取器。
            /// </summary>
            /// <returns></returns>
            public IDataReader ExecuteReader() => Add(command.ExecuteReader());

            /// <summary>
            /// 执行并生成读取器。
            /// </summary>
            /// <returns></returns>
            public IDataReader ExecuteReader(CommandBehavior behavior) => Add(command.ExecuteReader(behavior));

            /// <summary>
            /// 执行返回首行首列。
            /// </summary>
            /// <returns></returns>
            public object ExecuteScalar()
            {
                isAlive = true;

                try
                {
                    return command.ExecuteScalar();
                }
                finally
                {
                    isAlive = false;
                }
            }

            /// <summary>
            /// 准备就绪。
            /// </summary>
            public void Prepare() => command.Prepare();
        }
        /// <summary>
        /// 事务。
        /// </summary>
        private class DbTransaction : IDbTransaction
        {
            private readonly IDbTransaction transaction;

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="transaction"></param>
            public DbTransaction(IDbTransaction transaction)
            {
                this.transaction = transaction;
            }

            public IDbConnection Connection => transaction.Connection;

            public IsolationLevel IsolationLevel => transaction.IsolationLevel;

            public void Commit()
            {
                transaction.Commit();

                if (Connection is DbConnection connection)
                {
                    connection.Remove(this);
                }
            }

            public void Dispose()
            {
                transaction.Dispose();

                if (Connection is DbConnection connection)
                {
                    connection.Remove(this);
                }

                GC.SuppressFinalize(this);
            }

            public void Rollback()
            {
                transaction.Rollback();

                if (Connection is DbConnection connection)
                {
                    connection.Remove(this);
                }
            }
        }

        private DateTime lastUseTime;
        private DateTime lastActiveTime;
        private readonly IDbConnection _connection; //数据库连接
        private readonly double? connectionHeartbeat; //心跳
        private readonly List<DbCommand> commands = new List<DbCommand>();
        private readonly List<DbTransaction> transactions = new List<DbTransaction>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connection">数据库链接</param>
        public DbConnection(IDbConnection connection) => _connection = connection;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connection">数据库链接</param>
        /// <param name="connectionHeartbeat">链接心跳</param>
        public DbConnection(IDbConnection connection, double connectionHeartbeat) : this(connection)
        {
            lastUseTime = lastActiveTime = DateTime.Now;
            this.connectionHeartbeat = new double?(connectionHeartbeat);
        }

        /// <summary>
        /// 数据库连接
        /// </summary>
        public string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }

        /// <summary>
        /// 连接超时时间
        /// </summary>
        public int ConnectionTimeout => _connection.ConnectionTimeout;

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string Database => _connection.Database;

        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectionState State => _connection.State;

        /// <summary>
        /// 创建事务
        /// </summary>
        /// <returns></returns>
        public IDbTransaction BeginTransaction()
        {
            var transaction = new DbTransaction(_connection.BeginTransaction());

            transactions.Add(transaction);

            return transaction;
        }

        /// <summary>
        /// 创建事务
        /// </summary>
        /// <param name="il">隔离等级</param>
        /// <returns></returns>
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            var transaction = new DbTransaction(_connection.BeginTransaction(il));

            transactions.Add(transaction);

            return transaction;
        }

        /// <summary>
        /// 修改数据库
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        /// <summary>
        /// 打开链接
        /// </summary>
        public virtual void Open()
        {
            switch (State)
            {
                case ConnectionState.Closed:
                    _connection.Open();
                    break;
                case ConnectionState.Connecting:

                    do
                    {
                        Thread.Sleep(5);

                    } while (State == ConnectionState.Connecting);
                    break;
                case ConnectionState.Broken:
                    _connection.Close();
                    _connection.Open();
                    break;
            }
        }

        void IDbConnection.Close() => Close();

        private void Add(DbCommand command)
        {
            if (!commands.Contains(command))
            {
                commands.Add(command);
            }
        }

        private void Remove(DbCommand command)
        {
            commands.Remove(command);
        }

        private void Remove(DbTransaction transaction)
        {
            transactions.Remove(transaction);
        }

        /// <summary>
        /// 关闭链接
        /// </summary>
        public virtual void Close()
        {
            if (State == ConnectionState.Closed)
            {
                return;
            }

            if (connectionHeartbeat.HasValue && DateTime.Now <= lastActiveTime.AddMinutes(connectionHeartbeat.Value))
            {
                return;
            }

            _connection.Close();
        }

        /// <summary>
        /// 创建命令
        /// </summary>
        /// <returns></returns>
        public IDbCommand CreateCommand()
        {
            var command = new DbCommand(this, _connection.CreateCommand());

            commands.Add(command);

            lastActiveTime = DateTime.Now;

            return command;
        }

        /// <summary>
        /// 是否存活。
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (connectionHeartbeat.HasValue)
                {
                    return State == ConnectionState.Open && DateTime.Now <= lastActiveTime.AddMinutes(connectionHeartbeat.Value);
                }

                return State == ConnectionState.Open;
            }
        }

        /// <summary>
        /// 是否闲置。
        /// </summary>
        public bool IsIdle => transactions.Count == 0 && lastUseTime.AddMilliseconds(520D) < DateTime.Now && commands.TrueForAll(x => !x.IsAlive);

        /// <summary>
        /// 释放器不释放
        /// </summary>
        void IDisposable.Dispose() { }

        /// <summary>
        /// 复用。
        /// </summary>
        /// <returns></returns>
        public IDbConnection ReuseConnection()
        {
            lastUseTime = DateTime.Now;

            if (commands.Count > 0)
            {

            }

            return this;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <param name="disposing">确认释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection?.Close();
                _connection?.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }
}
