using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Core;
using Core.Data;
using Dapper;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;

namespace Data.Implement
{
    public abstract class GenericRepository<TEntity, TId> : IGenericRepository<TEntity, TId> where TEntity : class
    {
        private string _connectionString;

        public GenericRepository()
        {
            var dw = Directory.GetCurrentDirectory();
            var appSettingsPath = Directory.GetParent(dw).Parent.FullName + "\\appSettings.json";
            var json = File.ReadAllText(appSettingsPath);
            _connectionString = JsonConvert.DeserializeObject<AppSettings>(json).ConnectionString;
        }
        public TEntity GetById(TId id)
        {
            using (var dbCon = new OracleConnection(_connectionString))
            {
                dbCon.Open();
                return null;
            }
        }

        public List<TEntity> Get<TEntity>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {
            //_config.GetConnectionString("dbConnection"
            using (var dbCon = new OracleConnection(_connectionString))
            {
                dbCon.Open();
                //return dbCon.Query<TEntity>(sp, parms, commandType).ToList();
                return null;
            }
        }
    }
}
