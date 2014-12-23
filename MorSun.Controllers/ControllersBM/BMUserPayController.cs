using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MorSun.Model;
using HOHO18.Common;
using MorSun.Bll;
using System.Collections;
using System.Web.Mvc;
using MorSun.Controllers.ViewModel;
using System.Xml;
using MorSun.Common.Privelege;

namespace MorSun.Controllers.SystemController
{
    public class BMUserPayController : BaseController<bmUserPay>
    {
        protected override string ResourceId
        {
            get { return MorSun.Common.Privelege.资源.支付; }
        }

        protected override string OnAddCK(bmUserPay t)
        {
              
            return "";
        }

        protected override string OnEditCK(bmUserPay t)
        {                       
            return "";
        }        
    }
}
