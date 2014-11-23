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

namespace MorSun.Controllers.SystemController
{
    public class BMKaMeController : BaseController<bmKaMe>
    {
        protected override string ResourceId
        {
            get { return MorSun.Common.Privelege.资源.卡密; }
        }

        /// <summary>
        /// 批量增加卡密
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public ActionResult BeachAdd(BeachAddKaMe t, string returnUrl)
        {
            if (ResourceId.HP(操作.添加))
            {
                ViewBag.RS = ResourceId;
                ViewBag.ReturnUrl = returnUrl;                
                return View(t);
            }
            else
            {
                "".AE("无权限", ModelState);
                var oper = new OperationResult(OperationResultType.Error, "无权限");
                oper.AppendData = ModelState.GE();
                return Json(oper, JsonRequestBehavior.AllowGet);                
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BAdd(BeachAddKaMe t, string returnUrl)
        {
            if (ResourceId.HP(操作.添加))
            {
                var oper = new OperationResult(OperationResultType.Error, "生成失败");
                ViewBag.ReturnUrl = returnUrl;
                var bll = new BaseBll<wmfReference>();
                var r = bll.GetModel(t.KaMeRef);
                if (r == null)
                {
                    "KaMeRef".AE("无此类别", ModelState);
                }
                
                if (ModelState.IsValid)
                {
                    for (var i = 0; i < t.Num; i++)
                    {
                        var model = new bmKaMe();
                        model.ID = Guid.NewGuid();
                        CreateInitObject(model);
                        model.KaMeRef = t.KaMeRef;
                        model.KaMe = Guid.NewGuid().ToString().EP(Guid.NewGuid().ToString());
                        model.ImportRef = Guid.Parse(Reference.卡密导入_未导入);
                        model.Recharge = Guid.Parse(Reference.卡密充值_未充值);
                        Bll.Insert(model, false);
                    }
                    Bll.UpdateChanges();
                    fillOperationResult(returnUrl, oper, "生成成功");
                }
                else
                {
                    "".AE("修改失败", ModelState);
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

        /// <summary>
        /// 批量将卡密设为已导入
        /// </summary>
        /// <param name="t"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public ActionResult BeachImport(BeachAddKaMe t, string returnUrl)
        {
            if (ResourceId.HP(操作.修改))
            {
                ViewBag.RS = ResourceId;
                ViewBag.ReturnUrl = returnUrl;
                return View(t);
            }
            else
            {
                "".AE("无权限", ModelState);
                var oper = new OperationResult(OperationResultType.Error, "无权限");
                oper.AppendData = ModelState.GE();
                return Json(oper, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BImport(BeachAddKaMe t, string returnUrl)
        {
            if (ResourceId.HP(操作.修改))
            {
                var oper = new OperationResult(OperationResultType.Error, "批量导入失败");
                ViewBag.ReturnUrl = returnUrl;
                var bll = new BaseBll<wmfReference>();
                var r = bll.GetModel(t.KaMeRef);
                if (r == null)
                {
                    "KaMeRef".AE("无此类别", ModelState);
                }

                if (ModelState.IsValid)
                {
                    var ipref = Guid.Parse(Reference.卡密导入_未导入);
                    var list = Bll.All.Where(p => p.KaMeRef == t.KaMeRef && p.ImportRef == ipref);
                    foreach(var item in list)
                    {
                        item.ImportRef = Guid.Parse(Reference.卡密导入_已导入);                        
                    }                    
                    Bll.UpdateChanges();
                    fillOperationResult(returnUrl, oper, "批量导入成功");
                }
                else
                {
                    "".AE("修改失败", ModelState);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult JC(OperateKaMe t, string returnUrl)
        {
            if (ResourceId.HP(操作.修改))
            {
                var oper = new OperationResult(OperationResultType.Error, "检测失败");
                ViewBag.ReturnUrl = returnUrl;               
                if (String.IsNullOrEmpty(t.KaMe))
                {
                    "KaMe".AE("请选择卡密", ModelState);
                }

                if (ModelState.IsValid)
                {
                    var result = "";
                    string strUrl = CFG.网站域名 + CFG.卡密检测结果_检测地址 + t.KaMe;
                    LogHelper.Write("检测卡密访问" + strUrl, LogHelper.LogMessageType.Info);
                    result = GetHtmlHelper.GetPage(strUrl, "");                    
                    fillOperationResult(returnUrl, oper, "检测结果: " + result);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TK(OperateKaMe t, string returnUrl)
        {
            if (ResourceId.HP(操作.修改))
            {
                var oper = new OperationResult(OperationResultType.Error, "退款失败");
                ViewBag.ReturnUrl = returnUrl;               
                if (String.IsNullOrEmpty(t.KaMe))
                {
                    "KaMe".AE("请选择卡密", ModelState);
                }
                var rc = Guid.Parse(Reference.卡密充值_已退款);
                var model = Bll.All.FirstOrDefault(p => p.KaMe == t.KaMe);
                if (model == null)
                {
                    "KaMe".AE("请选择卡密", ModelState);
                }
                if(model != null && model.Recharge == rc)
                {
                    "KaMe".AE("该卡密已退款", ModelState);
                }

                if (ModelState.IsValid)
                {
                    var result = "";
                    var dt = DateTime.Now;
                    var dts = dt.ToShortDateString() + " " + dt.ToShortTimeString();
                    var tok = HttpUtility.UrlEncode(SecurityHelper.Encrypt(dts + ";" + "123"));
                    string strUrl = CFG.网站域名 + CFG.卡密退款_退款地址 + "?id=" + t.KaMe + "&tok=" + tok;
                    LogHelper.Write("卡密退款访问" + strUrl, LogHelper.LogMessageType.Info);
                    result = GetHtmlHelper.GetPage(strUrl, ""); 
                   
                    //根据结果来操作卡密
                    if (result == CFG.卡密退款_卡密退款操作成功)
                    {
                        model.Recharge = rc;
                        Bll.Update(model);
                        LogHelper.Write("卡密退款:" + t.KaMe, LogHelper.LogMessageType.Info);
                        fillOperationResult(returnUrl, oper, "退款成功: " + result);
                        return Json(oper, JsonRequestBehavior.AllowGet);
                    }
                    "".AE("退款失败: " + result, ModelState);
                    oper.AppendData = ModelState.GE();                    
                }
                else
                {
                    "".AE("退款失败", ModelState);
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
