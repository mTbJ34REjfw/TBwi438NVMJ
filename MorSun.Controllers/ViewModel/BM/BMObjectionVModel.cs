using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MorSun.Model;
using System.Web.Mvc;

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
                else
                {
                    l = l.Where(p => p.Result == null);
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

                if (!sIsSettle.HasValue)
                {
                    l = l.Where(p => p.IsSettle == null || p.IsSettle == false);
                }
                else
                {
                    l = l.Where(p => p.IsSettle == sIsSettle.Value);
                }
               
                return l.OrderBy(p => p.Sort).ThenBy(p => p.RegTime);
            }
        }

        public List<SelectListItem> SettleCollection
        {
            get
            {
                return new List<SelectListItem>() { 
                    //new SelectListItem(){Text="==审核状态==",Value=""},
                    new SelectListItem(){Text="未结算",Value="false"},
                    new SelectListItem(){Text="已结算",Value="true"}                                   
                };
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

        public virtual Boolean? sIsSettle { get; set; }

        public virtual string obIds { get; set; }

        public IQueryable<bmObjection> SearchList
        {
            get;
            set;
        }
    }
}
