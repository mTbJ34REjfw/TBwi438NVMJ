using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MorSun.Model;

namespace MorSun.Controllers.ViewModel
{
    public class BMObjectionVModel : BaseVModel<bmObjection>
    {
        public override IQueryable<bmObjection> List
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


        
        /// <summary>
        /// 被选中的编号
        /// </summary>
        public virtual string CheckedId { get; set; }

        public virtual string FlagTrashed { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public virtual DateTime? sStartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public virtual DateTime? sEndTime { get; set; }
        
        
    }
}
