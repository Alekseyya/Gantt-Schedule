using System.Collections.Generic;
using System.Data;
using Dapper;

namespace Core.Data
{
    public interface IGenericRepository<TEntity, TId>
    {
        TEntity GetById(TId id);
        List<TEntity> Get<TEntity>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure);
    }
}
