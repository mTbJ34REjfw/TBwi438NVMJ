using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MorSun.Model;

namespace MorSun.Controllers.ViewModel
{
    public class BMKaMeVModel : BaseVModel<bmKaMe>
    {
        public override IQueryable<bmKaMe> List
        {
            get
            {
                var l = All;
                if (String.IsNullOrEmpty(FlagTrashed))
                    FlagTrashed = "0";
                if (FlagTrashed == "1")
                {
                    l = l.Where(p => p.FlagTrashed == true);
                }
                if (FlagTrashed == "0")
                {
                    l = l.Where(p => p.FlagTrashed == false);
                }         
                if(sKaMeRef != null)
                {
                    l = l.Where(p => p.KaMeRef == sKaMeRef);
                }
                if(sImportRef != null)
                {
                    l = l.Where(p => p.ImportRef == sImportRef);
                }
                if(sRecharge != null)
                {
                    l = l.Where(p => p.Recharge == sRecharge);
                }
                if(sKaMeRef == null && sImportRef == null && sRecharge == null)
                {
                    l = l.Take(0);
                }

                if (sStartTime.HasValue)
                {
                    l = l.Where(p => p.RegTime >= sStartTime);
                }
                if (sEndTime.HasValue)
                {
                    l = l.Where(p => p.RegTime <= sEndTime);
                }
                return l.OrderBy(p => p.Sort).ThenBy(p => p.RegTime);
            }
        }



        public virtual Guid? sKaMeRef { get; set; }

        public virtual Guid? sImportRef { get; set; }

        public virtual Guid? sRecharge { get; set; }
        /// <summary>
        /// 被选中的编号
        /// </summary>
        public virtual string CheckedId { get; set; }

        public virtual string FlagTrashed { get; set; }

        public virtual DateTime? sStartTime { get; set; }

        public virtual DateTime? sEndTime { get; set; }
        
    }
}
