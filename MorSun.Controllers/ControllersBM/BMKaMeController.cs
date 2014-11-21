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
