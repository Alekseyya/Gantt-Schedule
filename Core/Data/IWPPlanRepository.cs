using System;
using System.Collections.Generic;
using Core.Model;

namespace Core.Data
{
    public interface IWPPlanRepository
    {
        int InsertTemporaryPlan(int beginOfPeriod, int endOfPeriod, string user, bool closePackageWork = false);
        List<PlanVersionHeaderDAO> GetTemporaryPlans();
        void UpdateTemporaryPlan(int identifierPlanVersion, StatusPlan statusPlan, string approvedUser);
        List<WPPlanDAO> GetByPL(string pl);
        PTO GetByPTO();
        List<WPPlanDAO> GetByPLAddDayByToday(int dayNumber);
        List<WPPlanDAO> GetByPLAddDayByToday(string pl, int dayNumber);
        List<WPPlanDAO> GetByPLByDates(string pl, DateTime dateStartTimeTO, DateTime dateEndTimeTO);
        List<WPPlanDAO> GetPlanByPLByDates(int planVersion, string pl, DateTime dateStartTimeTO, DateTime dateEndTimeTO, ACType acType);
        List<WPPlanDAO> GetPlanByPLByDates(string pl, DateTime dateStartTimeTO, DateTime dateEndTimeTO, ACType acType);
        List<WPPlanDAO> GetFactByPLByDates(string pl, DateTime dateStartTimeTO, DateTime dateEndTimeTO, ACType acType);
        List<WPPlanDAO> GetFactByPLByDates(int planVersion, string pl, DateTime dateStartTimeTO, DateTime dateEndTimeTO, ACType acType);
    }
}
