using Core.Model;
using Dapper.FluentMap.Mapping;

namespace Core.Mapping
{
    public class WpPlanDapperMapper : EntityMap<WpPlan>
    {
        public WpPlanDapperMapper()
        {
            Map(u => u.PLAN_VERSION_ID).ToColumn("PLAN_VERSION_ID");
            Map(u => u.WPNO_I).ToColumn("WPNO_I");
            Map(u => u.WPNO).ToColumn("WPNO");
            Map(u => u.ARRIVAL_AT).ToColumn("ARRIVAL_AT");
            Map(u => u.DEPARTURE_AT).ToColumn("DEPARTURE_AT");
            Map(u => u.INTERNAL_REMARKS).ToColumn("INTERNAL_REMARKS");
            Map(u => u.AC_TYP).ToColumn("AC_TYP");
            Map(u => u.AC_MODEL).ToColumn("AC_MODEL");
            Map(u => u.DESCRIPTION).ToColumn("DESCRIPTION");
            Map(u => u.STATION).ToColumn("STATION");
            Map(u => u.PROJECTNO).ToColumn("PROJECTNO");
            Map(u => u.MAINTPROV_ADDRESS).ToColumn("MAINTPROV_ADDRESS");
            Map(u => u.HIDDEN).ToColumn("HIDDEN");
            Map(u => u.AC_REGISTR).ToColumn("AC_REGISTR");
            Map(u => u.PL_START_AT).ToColumn("PL_START_AT");
            Map(u => u.ACT_START_AT).ToColumn("ACT_START_AT");
            Map(u => u.ACT_END_AT).ToColumn("ACT_END_AT");
            Map(u => u.PL_END_AT).ToColumn("PL_END_AT");
            Map(u => u.CREATED_DATE).ToColumn("CREATED_DATE");
            Map(u => u.MUTATION).ToColumn("MUTATION");
            Map(u => u.MUTATION_TIME).ToColumn("MUTATION_TIME");
            Map(u => u.MHR).ToColumn("MHR");
            Map(u => u.BOOKED_MHR).ToColumn("BOOKED_MHR");
            Map(u => u.ARRIVAL_DIF_DAYS).ToColumn("ARRIVAL_DIF_DAYS");
            Map(u => u.DEPARTURE_DIF_DAYS).ToColumn("DEPARTURE_DIF_DAYS");
            Map(u => u.CONTAINS_C_CHECK).ToColumn("CONTAINS_C_CHECK");
            Map(u => u.PARENT_WPNO_I).ToColumn("PARENT_WPNO_I");
        }
    }


    public class PlanVersionHeaderMapper : EntityMap<PlanVersionHeader>
    {
        public PlanVersionHeaderMapper()
        {
            Map(u => u.PLAN_VERSION_ID).ToColumn("PLAN_VERSION_ID");
            Map(u => u.DATE_FROM).ToColumn("DATE_FROM");
            Map(u => u.DATE_TO).ToColumn("DATE_TO");
            Map(u => u.STATUS).ToColumn("STATUS");
            Map(u => u.APPROVED_BY).ToColumn("APPROVED_BY");
            Map(u => u.APPROVED_AT).ToColumn("APPROVED_DATE");
            Map(u => u.CREATED_AT).ToColumn("CREATE_DATE");
            Map(u => u.CREATED_TIME).ToColumn("CREATE_TIME");
            Map(u => u.CREATED_BY).ToColumn("CREATE_BY");
        }
    }

}
