![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Net"是什么？

CodeArts.Net 是HTTP/HTTPS请求工具，涵盖了认证信息刷新、异常重试、结果验证与重发，结果反序列化、文件上传下载等功能。

#### 使用方式：

* 获得请求能力。

  - `String`.AsRequestable()
  - `Uri`.AsRequestable()

* 根据业务需要，按照提示即可下发请求指令。

  - 普通请求。

    ```c#
    string result = await "api".AsRequestable()
        .AppendQueryString("?{params}")
        .GetAsync();
    ```

  - 认证信息刷新请求。

    ```c#
    string result = await "api".AsRequestable()
        .AppendQueryString("?{params}")
        .AssignHeader("Authorization", "Bearer 3506555d8a256b82211a62305b6dx317")
        .TryThen((requestable, e) => { // 仅会执行一次，与其它重试机制无关。
            //TODO:刷新认证信息。
        })
        .If(e => e.Status == WebExceptionStatus.ProtocolError)
        .And(e => e.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
        .GetAsync();
    ```

  - 异常重试。

    ```c#
    string result = await "api".AsRequestable()
        .AppendQueryString("?{params}")
        .TryIf(e => e.Status == WebExceptionStatus.Timeout) // 重试条件。
        .Or(e => e.Status == WebExceptionStatus.UnknownError)
        .RetryCount(2) // 设置重试次数。不设置，默认重试一次。
        .RetryInterval(500)//重试间隔时长。
        .GetAsync();
    ```

    

  - 结果验证。

    ```c#
    ```

    

  

* 实体映射（任意类型之间的转换）。

  ``` c#
  public class MapTest
  {
      public int Id { get; set; }
      public string Name { get; set; }
  }
  public class MapToTest
  {
      public int Id { get; set; }
      public string Name { get; set; }
      public DateTime Date { get; set; }
  }
  
  var value = new MapToTest
  {
      Id = 1000,
      Name = "test",
      Date = DateTime.Now
  };
  // 不同类型的映射。
  var map1 = Mapper.Map<MapTest>(value);
  // 相同类型映射（复制实体）；
  var map2 = Mapper.Map<MapToTest>(value);
  ```

#### 接口契约：

```c#
    /// <summary>
    /// 配置文件帮助类。
    /// </summary>
    public interface IConfigHelper
    {
        ///<summary> 配置文件变更事件。</summary>
        event Action<object> OnConfigChanged;

        /// <summary>
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        T Get<T>(string key, T defaultValue = default);
    }
```

##### 说明：

* .NET Framework。
  - 运行环境包括：Web、Form、Service。
  - 运行环境默认使用`Web`。
  - 层级分隔符：`/`。
  - 默认读取`appStrings`下的键值。
  - 读取数据库连接:`connectionStrings/key`。
  - 读取数据库连接的连接字符：`connectionStrings/key/connectionString`。
  - 读取自定义`ConfigurationSectionGroup`请提供准确的类型，否则强转失败，返回默认值。
* .NET Standard：
  - 层级分隔符：`:`。
  - 读取规则与`Microsoft.Extensions.Configuration`保持一致。