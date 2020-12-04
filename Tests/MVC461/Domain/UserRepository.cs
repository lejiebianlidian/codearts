﻿using Mvc461.Domain.Entities;
using CodeArts.Db;

namespace Mvc461.Domain
{
    /// <summary>
    /// 用户仓库。
    /// </summary>
    [DbConfig]
    public class UserRepository : DbRepository<User>
    {
    }
}
