using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MorSun.Model;

namespace MorSun.Controllers.ViewModel
{
    public class BMUserPayVModel : BaseVModel<bmUserPay>
    {
        public override IQueryable<bmUserPay> List
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
                if (!String.IsNullOrEmpty(sALiPayNum))
                {
                    l = l.Where(p => p.ALiPayNum == sALiPayNum);
                }
                if (sBankRef != null && sBankRef != Guid.Empty)
                {
                    l = l.Where(p => p.BankRef == sBankRef);
                }
                if (!String.IsNullOrEmpty(sBankNum))
                {
                    l = l.Where(p => p.BankNum.Contains(sBankNum));
                }
                return l.OrderBy(p => p.Sort).ThenBy(p => p.RegTime);
            }
        }



        public virtual Guid? sBankRef { get; set; }
        /// <summary>
        /// 被选中的编号
        /// </summary>
        public virtual string CheckedId { get; set; }

        public virtual string sALiPayNum { get; set; }

        public virtual string FlagTrashed { get; set; }

        public virtual string sBankNum { get; set; }
        
    }
}
