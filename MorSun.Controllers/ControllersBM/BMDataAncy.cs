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
using MorSun.Common.类别;
using MorSun.Common.配置;
using HOHO18.Common.WEB;
using HOHO18.Common.Web;
using System.Web;
using HOHO18.Common.SSO;
using HOHO18.Common.DEncrypt;
using Newtonsoft.Json;

namespace MorSun.Controllers.SystemController
{
    public class BMDataAncyController : BaseController<bmKaMe>
    {
        protected override string ResourceId
        {
            get { return MorSun.Common.Privelege.资源.数据库备份; }
        }

        public ActionResult GetUJS(DateTime? SyncDT, string returnUrl)
        {
            if (ResourceId.HP(操作.修改))
            {
                var oper = new OperationResult(OperationResultType.Error, "获取失败");
                ViewBag.ReturnUrl = returnUrl;                

                if (ModelState.IsValid)
                {
                    //var neURLuids = SecurityHelper.Encrypt("e26ef963-ff8d-4569-b019-7fe16103c934,1479a879-3427-40b0-a697-b7385ad9aa6d");
                    AncyUser(SyncDT,"");
                    fillOperationResult(returnUrl, oper, "同步成功");
                }
                else
                {
                    "".AE("检测失败", ModelState);
                    oper.AppendData = ModelState.GE();
                }
                return Json(oper, JsonRequestBehavior.AllowGet);
            }
            else
            {
                "".AE("无权限", ModelState);
                var oper = new OperationResult(OperationResultType.Error, "无权限");
                oper.AppendData = ModelState.GE();
                return Json(oper, JsonRequestBehavior.AllowGet);
            }
        }        

        protected override string OnAddCK(bmKaMe t)
        {
              
            return "";
        }

        protected override string OnEditCK(bmKaMe t)
        {                       
            return "";
        }        
    }
}
