using System;
using System.Globalization;
using AutoMapper;
using Core.Model;

namespace Core.Mapping
{
    public class WpPlanProfile : Profile
    {
        public WpPlanProfile()
        {
            CreateMap<WpPlan, WPPlanDAO>()
                .ForMember(dao => dao.PLAN_VERSION_ID, e => e.MapFrom(o => o.PLAN_VERSION_ID))
                .ForMember(dao => dao.StartDate, e => e.MapFrom(o => ConvertToDateTime(o, o.PL_START_AT, nameof(o.PL_START_AT))))
                .ForMember(dao => dao.FactStartDate, e => e.MapFrom(o => ConvertToDateTime(o, o.ACT_START_AT, nameof(o.ACT_START_AT))))
                .ForMember(dao => dao.EndDate, e => e.MapFrom(o => ConvertToDateTime(o, o.PL_END_AT, nameof(o.PL_END_AT))))
                .ForMember(dao => dao.FactEndDate, e => e.MapFrom(o => ConvertToDateTime(o, o.ACT_END_AT, nameof(o.ACT_END_AT))))
                .ForMember(dao => dao.Arrival, e => e.MapFrom(o => ConvertToDateTime(o, o.ARRIVAL_AT, nameof(o.ARRIVAL_AT))))
                .ForMember(dao => dao.Departure, e => e.MapFrom(o => ConvertToDateTime(o, o.DEPARTURE_AT, nameof(o.DEPARTURE_AT))))
                .ForMember(dao => dao.MHR, e => e.MapFrom(o => o.MHR))
                .ForMember(dao => dao.BOOKED_MHR, e => e.MapFrom(o => o.BOOKED_MHR))
                .ForMember(dao => dao.AC_TYP, e => e.MapFrom(o => CovertToACTypeEnum(o)))
                .ForMember(dao => dao.AC_MODEL, e => e.MapFrom(o => CovertToACModelEnum(o)))
                .ForMember(dao => dao.CONTAINS_C_CHECK, e => e.MapFrom(o => ContainsCheckToBool(o.CONTAINS_C_CHECK)))
                .ForMember(dao => dao.WPNO, e => e.MapFrom(o => o.WPNO))
                .ForMember(dao => dao.WPNO_I, e => e.MapFrom(o => o.WPNO_I))
                .ForMember(dao => dao.STATION, e => e.MapFrom(o => o.STATION))
                .ForMember(dao => dao.STATION_NAME, e => e.MapFrom(o => o.STATION_NAME))
                .ForMember(dao => dao.Description, e => e.MapFrom(o => ConcatenateFields(o)))
                .ForMember(dao => dao.ParentWpno, e => e.MapFrom(o => o.PARENT_WPNO_I));
        }
        public DateTime ConvertToDateTime(WpPlan wpPlan, string date, string field)
        {
            DateTime.TryParseExact(date, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime selectDateTime);
            wpPlan.GetType().GetProperty(field).SetValue(wpPlan, selectDateTime.ToString("dd.MM.yyyy HH:mm:ss"));
            return Convert.ToDateTime(wpPlan.GetType().GetProperty(field).GetValue(wpPlan));
        }

        public bool ContainsCheckToBool(string check)
        {
            if (!string.IsNullOrEmpty(check) && check.Contains("Y"))
                return true;
            return false;
        }

        public string ConcatenateFields(WpPlan wpPlan)
        {
            var mhrPlan = wpPlan.MHR != 0 ? $" - {wpPlan.MHR} MH" : "";
            var remarks = !string.IsNullOrEmpty(wpPlan.INTERNAL_REMARKS) ? wpPlan.INTERNAL_REMARKS + "<br/>" : "";
            var name = $"{remarks}{wpPlan.AC_MODEL} {wpPlan.AC_REGISTR} {wpPlan.DESCRIPTION}{mhrPlan}";
            return name;
        }

        public ACType CovertToACTypeEnum(WpPlan wpPlan)
        {
            Enum.TryParse(wpPlan.AC_TYP, out ACType acType);
            return acType;
        }
        public ACModel CovertToACModelEnum(WpPlan wpPlan)
        {
            Enum.TryParse(wpPlan.AC_MODEL, out ACModel acModel);
            return acModel;
        }
    }

    public class PlanVersionHeaderProfile : Profile
    {
        public PlanVersionHeaderProfile()
        {
            CreateMap<PlanVersionHeader, PlanVersionHeaderDAO>()
                .ForMember(dao => dao.ID, e => e.MapFrom(o => o.PLAN_VERSION_ID))
                .ForMember(dao => dao.DateFrom, e => e.MapFrom(o => ConvertToDateTime(o, o.DATE_FROM, nameof(o.DATE_FROM))))
                .ForMember(dao => dao.DateTo, e => e.MapFrom(o => ConvertToDateTime(o, o.DATE_TO, nameof(o.DATE_TO))))
                .ForMember(dao => dao.ApprovedBy, e => e.MapFrom(o => o.APPROVED_BY))
                .ForMember(dao => dao.ApprovedDate, e => e.MapFrom(o => ConvertToDateTime(o, o.APPROVED_AT, nameof(o.APPROVED_AT))))
                .ForMember(dao => dao.CreatedDate, e => e.MapFrom(o => ConvertToDateTime(o, o.CREATED_AT, nameof(o.CREATED_TIME))))
                .ForMember(dao => dao.Status, e => e.MapFrom(o => ConvertToStatusPlan(o.STATUS)))
                .ForMember(dao => dao.CreatedBy, e => e.MapFrom(o => o.CREATED_BY));
        }

        public DateTime ConvertToDateTime(PlanVersionHeader planVersion, string date, string field)
        {
            DateTime.TryParseExact(date, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime selectDateTime);
            planVersion.GetType().GetProperty(field).SetValue(planVersion, selectDateTime.ToString("dd.MM.yyyy HH:mm:ss"));
            return Convert.ToDateTime(planVersion.GetType().GetProperty(field).GetValue(planVersion));
        }

        private StatusPlan ConvertToStatusPlan(string status)
        {
            Enum.TryParse(status, out StatusPlan statusPlan);
            return statusPlan;
        }
    }
}
