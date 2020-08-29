using System.Collections.Generic;

namespace Core.Model
{
    public class PTO
    {
        /// <summary>
        /// C-ch
        /// </summary>
        public List<WPPlanDAO> C_Ch { get; set; }
        /// <summary>
        /// ПТО силами сторонних провайдеров
        /// </summary>
        public List<WPPlanDAO> PTOPartyProviders { get; set; }
        /// <summary>
        /// ОТО ангар
        /// </summary>
        public List<WPPlanDAO> OTOAngar { get; set; }
        /// <summary>
        /// AOG, A-ch
        /// </summary>
        public List<WPPlanDAO> AOG { get; set; }
        /// <summary>
        /// Мойки ВС
        /// </summary>
        public List<WPPlanDAO> Washing { get; set; }
    }
}
