﻿using CodeArts.Db;
using CodeArts.Db.EntityFramework;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace UnitTest.Domain
{
    [DefaultDbConfig]
    public class AuthShipRepository : Repository<AuthShip>
    {
    }
}
