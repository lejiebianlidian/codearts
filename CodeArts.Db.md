![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Db"是什么？

CodeArts.Db 是包含数据库连接池、事务池、以及SQL分析器的高扩展性轻量级框架。

#### 使用方式：

* 连接池。

  ```c#
      /// <summary>
      /// 数据连接工程。
      /// </summary>
      public interface IDbConnectionFactory
      {
          /// <summary> 创建数据库连接。 </summary>
          /// <returns></returns>
          IDbConnection Create(string connectionString);
  
          /// <summary>
          /// 连接类型。
          /// </summary>
          Type DbConnectionType { get; }
  
          /// <summary>
          /// 供应器名称。
          /// </summary>
          string ProviderName { get; }
  
          /// <summary>
          /// 链接心跳（链接可以在心跳活动时间内被重用不需要重新分配链接，单位：分钟）。
          /// </summary>
          double ConnectionHeartbeat { get; }
  
          /// <summary>
          /// 线程池数量。
          /// </summary>
          int MaxPoolSize { get; }
      }
  ```

  - 获取链接。

    ```c#
    IDbConnection connection = DispatchConnections.GetConnection(string,IDbConnectionFactory[,bool]);
    ```

  - 生命周期：连接最后一次使用时间超过心跳时间，连接会自动关闭和释放。

* 事务池。

  - 获取链接。

    ```c#
    IDbConnection connection = TransactionConnections.GetConnection(string,IDbConnectionFactory);
    ```

  - 生命周期：`Transaction`提交或释放。

* 数据库实体基础接口：`IEntiy`,`IEntiy<TKey>`。

* 数据库实体基础类：`BaseEntity`,`BaseEntity<TKey>`。

* SQL。

  ```c#
      /// <summary>
      /// SQL 适配器。
      /// </summary>
      public interface ISqlAdpter
      {
          /// <summary>
          /// SQL 分析。
          /// </summary>
          /// <param name="sql">语句。</param>
          /// <returns></returns>
          string Analyze(string sql);
  
          /// <summary>
          /// SQL 分析（表名称）。
          /// </summary>
          /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
          /// <returns></returns>
          IReadOnlyCollection<TableToken> AnalyzeTables(string sql);
  
          /// <summary>
          /// SQL 分析（参数）。
          /// </summary>
          /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
          /// <returns></returns>
          IReadOnlyCollection<string> AnalyzeParameters(string sql);
  
          /// <summary>
          /// 获取符合条件的条数。
          /// </summary>
          /// <param name="sql">SQL</param>
          /// <example>SELECT * FROM Users WHERE Id > 100 => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
          /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
          /// <returns></returns>
          string ToCountSQL(string sql);
  
          /// <summary>
          /// 生成分页SQL。
          /// </summary>
          /// <param name="sql">SQL</param>
          /// <param name="pageIndex">页码（从“0”开始）</param>
          /// <param name="pageSize">分页条数</param>
          /// <example>SELECT * FROM Users WHERE Id > 100 => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>)</example>
          /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>,`ORDER BY Id DESC`)</example>
          /// <returns></returns>
          string ToSQL(string sql, int pageIndex, int pageSize);
  
          /// <summary>
          /// SQL 格式化（格式化为数据库可执行的语句）。
          /// </summary>
          /// <param name="sql">语句。</param>
          /// <returns></returns>
          string Format(string sql);
  
          /// <summary>
          /// SQL 格式化（格式化为数据库可执行的语句）。
          /// </summary>
          /// <param name="sql">语句。</param>
          /// <param name="settings">配置。</param>
          /// <returns></returns>
          string Format(string sql, ISQLCorrectSettings settings);
      }
  ```

##### 说明：

* 基础请求配置。
  - `AssignHeader`设置求取头。
  - `AppendQueryString`添加请求参数。
    - 正常情况：多次添加相同的参数名称，不会被覆盖（数组场景）。
    - 刷新认证：`TryThen`函数中，会覆盖相同名称的参数。
* 请求方式。
  - 显示支持：GET、DELETE、POST、PUT、HEAD、PATCH。
  - 隐式支持：使用`Request`/`RequestAsync`方法，第一个参数为请求方式。
  - 文件处理：`UploadFile`/`UploadFileAsync`文件上传，`DownloadFile`/`DownloadFileAsync`文件下载。

* 数据传输。
  - Json：`content-type = "application/json"`。
  - Xml：`content-type = "application/xml"`。
  - Form：`content-type = "application/x-www-form-urlencoded"`。
  - Body：自己序列化数据和指定`content-type`。
* 数据接收。
  - XmlCast&lt;T&gt;：接收Xml格式数据，并自动反序列化为`T`类型。
  - JsonCast&lt;T&gt;：接收JSON格式数据，并自动反序列化为`T`类型，需要提供`IJsonHelper`接口支持，可以使用`CodeArts.Json`包。
  - String：接收任意格式结果。
* 刷新认证。
  - Then/ThenAsync/TryThen/TryThenAsync：请求异常刷新认证(每个设置，最多执行异常)。
  - If/And：需要刷新认证的条件。
* 重试。
  - TryIf/Or：重试条件（返回`true`代表需要重试）。
  - RetryCount：重试次数，默认：1次。
  - RetryInterval：重试时间间隔，默认：异常立即重试。
* 数据验证。
  - DataVerify/And：数据验证（返回`true`代表数据符合预期，不需要重试）。
  - ResendCount：重发次数，默认：1次。
  - ResendInterval：重发时间间隔，默认：异常立即重试。
* 其它。
  - WebCatch：捕获`WebException`异常，会继续抛出`WebException`异常。
  - WebCatch&lt;T&gt;：捕获`WebException`异常，并返回`T`结果，不抛异常。
  - XmlCatch：捕获`XmlException`异常，会继续抛出`XmlException`异常。
  - XmlCatch&lt;T&gt;：捕获`XmlException`异常，并返回`T`结果，不抛异常。
  - JsonCatch：捕获`JsonException`异常，会继续抛出`JsonException`异常。
  - JsonCatch&lt;T&gt;：捕获`JsonException`异常，并返回`T`结果，不抛异常。
  - Finally：请求完成（执行一次）。
  - UseEncoding：数据编码格式，默认：`UTF8`。