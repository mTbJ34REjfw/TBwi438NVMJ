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

                if(!String.IsNullOrEmpty(sUserName))
                {
                    l = l.Where(p => p.aspnet_Users1.UserName.Contains(sUserName));
                }
                  
                if(sResult.HasValue)
                {
                    l = l.Where(p => p.Result == sResult);
                }

                if (sStartTime.HasValue)
                {
                    l = l.Where(p => p.SubmitTime >= sStartTime);
                }
                if (sEndTime.HasValue)
                {
                    l = l.Where(p => p.SubmitTime <= sEndTime);
                }

                if (hStartTime.HasValue)
                {
                    l = l.Where(p => p.HandleTime >= sStartTime);
                }
                if (hEndTime.HasValue)
                {
                    l = l.Where(p => p.HandleTime <= sEndTime);
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
        /// 提交异议开始时间
        /// </summary>
        public virtual DateTime? sStartTime { get; set; }

        /// <summary>
        /// 提交异议结束时间
        /// </summary>
        public virtual DateTime? sEndTime { get; set; }

        public virtual string sUserName { get; set; }

        /// <summary>
        /// 处理异议开始时间
        /// </summary>
        public virtual DateTime? hStartTime { get; set; }

        /// <summary>
        /// 处理异议结束时间
        /// </summary>
        public virtual DateTime? hEndTime { get; set; }

        public virtual Guid? sResult { get; set; }
    }
}
