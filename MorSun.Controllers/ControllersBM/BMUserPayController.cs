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
using MorSun.Common.配置;
using MorSun.Common.类别;
using HOHO18.Common.SSO;
using HOHO18.Common.WEB;
using HOHO18.Common.Web;

namespace MorSun.Controllers.SystemController
{
    public class BMUserPayController : BaseController<bmUserPay>
    {
        protected override string ResourceId
        {
            get { return MorSun.Common.Privelege.资源.支付; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RZUser(bmUserPay uPay, string returnUrl)
        {
            var oper = new OperationResult(OperationResultType.Error, "提交失败");
            if (ModelState.IsValid)
            {
                var uPaybll = new BaseBll<bmUserPay>();
                var model = uPaybll.GetModel(uPay.ID);
                var roleId = CFG.注册默认角色;
                var ancyrz = "false";
                if(model.CertificationRef == Guid.Parse(Reference.答题认证情况_认证))
                { 
                    roleId = CFG.作业邦认证默认角色;
                    ancyrz = "true";
                }
                //先删除用户的角色，再添加
                var constr = "";
                var roleBll = new BaseBll<aspnet_Roles>();                
                constr += @"DELETE FROM [aspnet_UsersInRoles] WHERE [UserId] = '" + model.UserId + "'";
                constr += @"Insert Into aspnet_UsersInRoles ([UserId],[RoleId])  VALUES ('" + model.UserId + "','" + roleId + "')";

                var bll = new BaseBll<wmfUserInfo>();
                var rzUR = Guid.Parse(Reference.认证类别_认证邦主);
                var nonrzUR = Guid.Parse(Reference.认证类别_未认证);

                var uinfo = bll.GetModel(uPay.UserId);

                if (uinfo != null)
                {
                    if (model.CertificationRef == Guid.Parse(Reference.答题认证情况_认证))
                        uinfo.CertificationLevel = rzUR;
                    else
                        uinfo.CertificationLevel = nonrzUR;
                }
                bll.UpdateChanges();

                roleBll.Db.ExecuteStoreCommand(constr);

                var _rzUserId = new List<Guid>();
                _rzUserId.Add(model.UserId.Value);
                var s = "";
                if (_rzUserId.Count() == 0)
                {
                    s += " ";
                }
                else
                {
                    s += ToJsonAndCompress(_rzUserId);
                }
                s += CFG.邦马网_JSON数据间隔;
                s += ToJsonAndCompress(ancyrz);
                s += CFG.邦马网_JSON数据间隔;

                //向邦马网同步马币充值数据
                var result = "";
                var dts = DateTime.Now.ToString();
                var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                var strUrl = CFG.网站域名 + CFG.数据同步_认证改角色;

                //未Encode的URL
                string neAppentUrl = "?tok=" + tok;
                if (!string.IsNullOrEmpty(s))
                {
                    neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                }

                LogHelper.Write("同步马币充值信息" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                //有传递UID时用POST方法，参数有可能会超过URL长度
                result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                if(result == "true")
                { 
                    //封装返回的数据
                    fillOperationResult(Url.Action("Index", "MM"), oper, "修改成功");
                    return Json(oper, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    oper.AppendData = ModelState.GE();
                    return Json(oper, JsonRequestBehavior.AllowGet);
                }
            }
            oper.AppendData = ModelState.GE();
            return Json(oper, JsonRequestBehavior.AllowGet);
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
