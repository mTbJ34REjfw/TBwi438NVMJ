﻿using System;
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
using MorSun.Controllers.Filter;

namespace MorSun.Controllers.SystemController
{
    public class BMObjectionController : BaseController<bmObjection>
    {
        protected override string ResourceId
        {
            get { return MorSun.Common.Privelege.资源.异议处理; }
        }

        /// <summary>
        /// 显示编辑页面
        /// </summary>
        /// <param name="t">实体类</param>
        /// <returns></returns>
        [Authorize]
        [ValidateInput(false)]
        [ExceptionFilter()]
        public virtual ActionResult HDOB(Guid? id, string returnUrl)
        {
            if (ResourceId.HP(操作.修改))
            {
                var model = new BMQAViewVModel();
                if (id != null)
                {
                    ViewBag.RS = ResourceId;
                    ViewBag.ReturnUrl = returnUrl;
                    model.sParentId = id;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("I", "H");
                }
            }
            else
            {
                "".AE("无权限", ModelState);
                var oper = new OperationResult(OperationResultType.Error, "无权限");
                oper.AppendData = ModelState.GE();
                return Json(oper, JsonRequestBehavior.AllowGet);
            }

        }

        [Authorize]
        [ValidateInput(false)]
        [ExceptionFilter()]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual ActionResult HDOB(HandleObjection t, string returnUrl)
        {
            var oper = new OperationResult(OperationResultType.Error, "提交失败");
            var model = Bll.GetModel(t.ID);
            var qaView = new bmQAView();
            if (model == null)
                "ErrorNum".AE("未找到该条记录", ModelState);
            else 
            { 
                var bmqaBll = new BaseBll<bmQAView>();
                qaView = bmqaBll.GetModel(model.QAId);
                if(qaView == null || qaView.ID == null)
                    "ErrorNum".AE("未找到该条异议的问题记录", ModelState);
            }
            if (ModelState.IsValid)
            {
                //异议更新处理
                TryUpdateModel(model);

                model.ModTime = DateTime.Now;
                model.HandleUser = Guid.Parse(CFG.异议处理用户);
                model.HandleTime = DateTime.Now;

                //马币处理
                //如果问题回答正确，或问题没有标准答案并且用户回答的也是正确的一种，则判断也是正确。
                //需要传递的马币Json
                var umbrListJson = new List<bmUserMaBiRecordJson>();
                var umbrList = new List<bmUserMaBiRecord>();

                //判断问题的回答情况
                var qResult = t.Result.ToString();
                switch(qResult)
                {
                    case Reference.异议处理结果_答错:
                        //扣取答题用户的邦马币
                        var qaMB = Math.Abs(qaView.MBNum);
                        var qaBB = Math.Abs(qaView.BBNum);

                        var mbRef = Guid.Parse(Reference.马币类别_马币);
                        var bbRef = Guid.Parse(Reference.马币类别_邦币);
                        var banbRef = Guid.Parse(Reference.马币类别_绑币);

                        var mbly_kq = Guid.Parse(Reference.马币来源_扣取);
                        var mbly_gh = Guid.Parse(Reference.马币来源_归还);
                
                        var umbrBll = new BaseBll<bmUserMaBiRecord>();
                

                        var umbrModel = new bmUserMaBiRecord();
                        umbrModel.ID = Guid.NewGuid();
                        umbrModel.SourceRef = Guid.Parse(Reference.马币来源_取现);
                        umbrModel.MaBiRef = mbRef;
                        umbrModel.MaBiNum = 0 - ct.MaBiNum;
                        umbrModel.TkId = ct.ID;
                        umbrModel.UserId = ct.UserId;

                        umbrModel.IsSettle = false; //取现马币时，都是未结算的。取现之后，还是未结算。等每天自动结算时再结算
                        umbrModel.RegTime = DateTime.Now;
                        umbrModel.ModTime = DateTime.Now;
                        umbrModel.FlagTrashed = false;
                        umbrModel.FlagDeleted = false;
                        umbrModel.RegUser = ct.UserId;
                        //数据库马币记录添加
                        umbrList.Add(umbrModel);
                        //Json马币数据添加
                        umbrListJson.Add(new bmUserMaBiRecordJson
                        {
                            ID = umbrModel.ID,
                            UserId = umbrModel.UserId,
                            TkId = umbrModel.TkId,
                            SourceRef = umbrModel.SourceRef,
                            MaBiRef = umbrModel.MaBiRef,
                            MaBiNum = umbrModel.MaBiNum,
                            IsSettle = umbrModel.IsSettle,

                            RegUser = umbrModel.RegUser,
                            RegTime = umbrModel.RegTime,
                            ModTime = umbrModel.ModTime,
                            FlagTrashed = umbrModel.FlagTrashed,
                            FlagDeleted = umbrModel.FlagDeleted
                        });

                        break;

                    case Reference.异议处理结果_答对:
                        break;

                    case Reference.异议处理结果_无标准答案:
                        break;
                }

                



                //数据传递处理 
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
                if (result == "true")
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
        
        protected override string OnAddCK(bmObjection t)
        {              
            return "";
        }

        protected override string OnEditCK(bmObjection t)
        {                       
            return "";
        }        
    }
}
