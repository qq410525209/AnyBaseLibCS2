using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnyBaseLib.Bases;

namespace AnyBaseLib
{
    public static class CAnyBase
    {
        public static IAnyBase Base(string type)
        {
            return type.ToLower() switch
            {
                "mysql" => new Bases.MySQLDriver(),
                "sqlite" => new Bases.SQLiteDriver(),
                "postgre" => new Bases.PostgreDriver(),
                _ => throw new Exception($"Unknown database type: {type}"),
            };
        }

        public static int Version()
        { return 9; }
    }
}
