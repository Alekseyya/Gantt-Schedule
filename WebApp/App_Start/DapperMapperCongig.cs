using Core.Mapping;
using Dapper.FluentMap;

namespace WebApp
{
    public class DapperMapperConfig
    {
        public static void Init()
        {
            FluentMapper.Initialize(conf =>
            {
                conf.AddMap(new WpPlanDapperMapper());
                conf.AddMap(new PlanVersionHeaderMapper());
            });
        }
    }
}