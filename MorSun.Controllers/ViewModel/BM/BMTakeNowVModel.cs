using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MorSun.Model;

namespace MorSun.Controllers.ViewModel
{
    public class BMTakeNowVModel : BaseVModel<bmTakeNow>
    {
        public override IQueryable<bmTakeNow> List
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
                if (sEffective.HasValue)
                {
                    l = l.Where(p => p.Effective == sEffective.Value);
                }
                if (sTakeRef.HasValue)
                {
                    l = l.Where(p => p.TakeRef == sTakeRef.Value);
                }                
                if(sTakeTimeStar.HasValue)
                {
                    l = l.Where(p => p.TakeTime >= sTakeTimeStar.Value);
                }
                if(sTakeTimeEnd.HasValue)
                {
                    l = l.Where(p => p.TakeTime <= sTakeTimeEnd.Value);
                }
                if (sRegTimeStar.HasValue)
                {
                    l = l.Where(p => p.TakeTime >= sRegTimeStar.Value);
                }
                if (sRegTimeEnd.HasValue)
                {
                    l = l.Where(p => p.TakeTime <= sRegTimeEnd.Value);
                }
                if(!String.IsNullOrEmpty(sUserName))
                {
                    l = l.Where(p => p.aspnet_Users1.UserName.Contains(sUserName));
                }
                return l.OrderBy(p => p.Sort).ThenBy(p => p.RegTime);
            }
        }



        public virtual Guid? sEffective { get; set; }

        public virtual Guid? sTakeRef { get; set; }
        /// <summary>
        /// 被选中的编号
        /// </summary>
        public virtual string CheckedId { get; set; }

        public virtual Guid? sUserId { get; set; }

        public virtual string sUserName { get; set; }

        public virtual string FlagTrashed { get; set; }

        public virtual DateTime? sTakeTimeStar { get; set; }
        public virtual DateTime? sTakeTimeEnd { get; set; }

        public virtual DateTime? sRegTimeStar { get; set; }
        public virtual DateTime? sRegTimeEnd { get; set; }
        
    }
}
