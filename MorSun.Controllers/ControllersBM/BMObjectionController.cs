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
                "ConfirmErrorNum".AE("未找到该条记录", ModelState);
            else 
            { 
                var bmqaBll = new BaseBll<bmQAView>();
                qaView = bmqaBll.GetModel(model.QAId);
                if(qaView == null || qaView.ID == null)
                    "ConfirmErrorNum".AE("未找到该条异议的问题记录", ModelState);
            }
            if(model.IsSettle != null && model.IsSettle.Value)
            {
                "ConfirmErrorNum".AE("该异议已经结算，您不能再处理异议", ModelState);
            }
            //提问用户与答题用户取出来
            var bmuwBll = new BaseBll<bmUserWeixin>();
            var qaUser = bmuwBll.All.FirstOrDefault(p => p.WeiXinId == qaView.WeiXinId);
            var disUser = bmuwBll.All.FirstOrDefault(p => p.WeiXinId == qaView.DisWeiXinId);
            if(qaUser == null)
            {
                "ConfirmErrorNum".AE("提问用户未绑定", ModelState);
            }
            else if (qaUser.UserId != model.UserId)
            {
                "ConfirmErrorNum".AE("异议提交人不是提问人", ModelState);
            }

            if(disUser == null)
            {
                "ConfirmErrorNum".AE("问题回答人员不存在", ModelState);
            }
            //确认错题数量大于提交的错题数量
            if(t.ConfirmErrorNum > model.ErrorNum)
            {
                "ConfirmErrorNum".AE("确认错题数量大于提交值", ModelState);
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

                var umbrBll = new BaseBll<bmUserMaBiRecord>();

                //判断问题的回答情况
                var qResult = t.Result.ToString();
                switch(qResult)
                {
                    case Reference.异议处理结果_答错:

                        //过滤掉已经添加的数据                    
                        var alreadyMB = umbrBll.All.Where(p => p.QAId != null && p.DisId != null && p.OBId != null && p.OBId == model.ID);
                        foreach (var d in alreadyMB)
                        {
                            umbrBll.Delete(d, false);
                        }
                        umbrBll.UpdateChanges();

                        //扣取答题用户的邦马币
                        var qaMB = Math.Abs(qaView.MBNum);
                        var qaBB = Math.Abs(qaView.BBNum);

                        var mbRef = Guid.Parse(Reference.马币类别_马币);
                        var bbRef = Guid.Parse(Reference.马币类别_邦币);
                        var banbRef = Guid.Parse(Reference.马币类别_绑币);

                        var mbly_kq = Guid.Parse(Reference.马币来源_扣取);
                        var mbly_gh = Guid.Parse(Reference.马币来源_归还);

                        //平均每道题目消费的邦马币值,去除小数点后的数
                        var mbEVQ = Math.Floor((qaMB + qaBB) / t.AllQANum);                         
                        //提问消费邦马币占比
                        var bbPer = qaBB / (qaMB + qaBB);
                        var mbPer = qaMB / (qaMB + qaBB);
                                                

                        //答题用户答错一道题要扣的邦马币值
                        var kqBMB = Convert.ToDecimal(CFG.答错扣取的马币比) * mbEVQ;

                        //总的要扣取答题用户的邦马币值
                        var kqDISUserAllB = 0 - kqBMB * t.ConfirmErrorNum;
                        //扣取的绑币值
                        var kqDISUserBanB = kqDISUserAllB * bbPer;
                        //扣取的马币值
                        var kqDISUserMb = kqDISUserAllB * mbPer;


                        //总的要归还提问用户的邦马币值
                        var ghQAUserAllB = mbEVQ * t.ConfirmErrorNum;
                        //归还的邦币值
                        var ghQAUserBB = ghQAUserAllB * bbPer;
                        //归还的马币值
                        var ghQAUserMB = ghQAUserAllB * mbPer;

                        var obViewBll = new BaseBll<bmOBView>();
                        var obViewModel = obViewBll.All.FirstOrDefault(p => p.ID == model.ID);

                        var obMB = Math.Abs(obViewModel.KQMBNUM);
                        var obBB = Math.Abs(obViewModel.KQBBNum);

                        //每题要归还的压金值 做成按扣取的压金总值除以总问题数
                        var ghEVO = Math.Floor((obMB + obBB) / t.AllQANum);

                        //邦马币平均值
                        var obbbPer = obBB / (obMB + obBB);
                        var obmbPer = obMB / (obMB + obBB);
                        //总的要归还提问用户的压金值
                        var ghQAUserAllYJ = ghEVO * t.ConfirmErrorNum;
                        
                        //归还的压金邦币值//要先求出压金的比率
                        var ghQAUserBBYJ = ghQAUserAllYJ * obbbPer;
                        var ghQAUserMBYJ = ghQAUserAllYJ * obmbPer;

                        //生成扣款记录 有绑币扣款与马币扣款
                        if (Math.Abs(kqDISUserBanB) > 0)
                        { 
                            AddBMB(model, qaView, disUser, umbrListJson, umbrList, banbRef, mbly_kq, kqDISUserBanB);
                        }
                        if (Math.Abs(kqDISUserMb) > 0)
                        { 
                            AddBMB(model, qaView, disUser, umbrListJson, umbrList, mbRef, mbly_kq, kqDISUserMb);
                        }

                        //生成提问归还记录 有邦币归还与马币归还
                        if (Math.Abs(ghQAUserBB) > 0)
                        { 
                            AddBMB(model, qaView, qaUser, umbrListJson, umbrList, bbRef, mbly_gh, ghQAUserBB);
                        }
                        if (Math.Abs(ghQAUserMB) > 0)
                        {
                            AddBMB(model, qaView, qaUser, umbrListJson, umbrList, mbRef, mbly_gh, ghQAUserMB);
                        }
                        //生成压金归还记录 有邦币归还与马币归还
                        if (Math.Abs(ghQAUserBBYJ) > 0)
                        {
                            AddBMB(model, qaView, qaUser, umbrListJson, umbrList, bbRef, mbly_gh, ghQAUserBBYJ);
                        }
                        if (Math.Abs(ghQAUserMBYJ) > 0)
                        {
                            AddBMB(model, qaView, qaUser, umbrListJson, umbrList, mbRef, mbly_gh, ghQAUserMBYJ);
                        }

                        break;

                    case Reference.异议处理结果_答对:
                        break;

                    case Reference.异议处理结果_无标准答案:
                        break;
                }   

                //数据传递处理 
                var bmOBJson = new bmObjectionJson{
                    ID = model.ID,
                    QAId = model.QAId,
                    WeiXinId = model.WeiXinId,
                    SubmitTime = model.SubmitTime,
                    ErrorNum = model.ErrorNum,
                    ObjectionExplain = model.ObjectionExplain,
                    HandleUser = model.HandleUser,
                    AllQANum = model.AllQANum,
                    ConfirmErrorNum = model.ConfirmErrorNum,
                    HandleTime = model.HandleTime,
                    Result = model.Result,
                    HandleExplain = model.HandleExplain,
                    IsSettle = model.IsSettle,
                    Sort = model.Sort,
                    RegUser = model.RegUser,
                    RegTime = model.RegTime,
                    ModTime = model.ModTime,
                    FlagTrashed = model.FlagTrashed,
                    FlagDeleted = model.FlagDeleted                
                };
                
                var s = "";
                if (model == null)
                {
                    s += " ";
                }
                else
                {
                    s += ToJsonAndCompress(bmOBJson);
                }
                s += CFG.邦马网_JSON数据间隔;

                if (umbrListJson.Count() == 0)
                {
                    s += " ";
                }
                else
                {
                    s += ToJsonAndCompress(umbrListJson);
                }                
                s += CFG.邦马网_JSON数据间隔;

                //向邦马网同步马币充值数据
                var result = "";
                var dts = DateTime.Now.ToString();
                var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                var strUrl = CFG.网站域名 + CFG.数据同步_异议处理;

                //未Encode的URL
                string neAppentUrl = "?tok=" + tok;
                if (!string.IsNullOrEmpty(s))
                {
                    neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                }

                LogHelper.Write("同步异议处理信息" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                //有传递UID时用POST方法，参数有可能会超过URL长度
                result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                if (result == "true")
                {
                    //封装返回的数据
                    fillOperationResult(Url.Action("Index", "BMObjection"), oper, "处理成功");
                    Bll.UpdateChanges();
                    if (umbrList.Count() > 0)
                    {
                        foreach(var umb in umbrList)
                        {
                            umbrBll.Insert(umb);
                        }
                        umbrBll.UpdateChanges();
                    }
                    //发送邮件功能
                    var mrbll = new BaseBll<wmfMailRecord>();
                    var qaU = model.bmQA.aspnet_Users1;
                    var disU = new BaseBll<aspnet_Users>().All.FirstOrDefault(p => p.UserId == disUser.UserId);
                    //提问人员发送邮件
                    try
                    {
                        OBQAMail(mrbll, qaU.UserName, qaU.wmfUserInfo.NickName, model.QAId.ToSecureString(), model.bmQA.AutoGrenteId.ToString());
                    }
                    catch
                    {
                        LogHelper.Write("异议处理发邮件不成功 异议记录ID：" + model.ID, LogHelper.LogMessageType.Info);
                    }
                    //答题人员发送邮件
                    try
                    {
                        OBQAMail(mrbll, disU.UserName, disU.wmfUserInfo.NickName, model.QAId.ToSecureString(), model.bmQA.AutoGrenteId.ToString());
                    }
                    catch
                    {
                        LogHelper.Write("异议处理发邮件不成功 异议记录ID：" + model.ID, LogHelper.LogMessageType.Info);
                    }

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

        /// <summary>
        /// 生成扣取或归还的邦马币值
        /// </summary>
        /// <param name="model"></param>
        /// <param name="qaView"></param>
        /// <param name="disUser"></param>
        /// <param name="umbrListJson"></param>
        /// <param name="umbrList"></param>
        /// <param name="banbRef"></param>
        /// <param name="mbly_kq"></param>
        /// <param name="kqDISUserBanB"></param>
        private static void AddBMB(bmObjection model, bmQAView qaView, bmUserWeixin disUser, List<bmUserMaBiRecordJson> umbrListJson, List<bmUserMaBiRecord> umbrList, Guid mbRef, Guid mbly_kq, decimal kqDISUserBanB)
        {
            var umbrModel = new bmUserMaBiRecord();
            umbrModel.ID = Guid.NewGuid();
            umbrModel.SourceRef = mbly_kq;
            umbrModel.MaBiRef = mbRef;
            umbrModel.MaBiNum = kqDISUserBanB;
            umbrModel.QAId = model.QAId;
            umbrModel.DisId = qaView.DisId;
            umbrModel.OBId = model.ID;
            umbrModel.UserId = disUser.UserId;

            umbrModel.IsSettle = false; //取现马币时，都是未结算的。取现之后，还是未结算。等每天自动结算时再结算
            umbrModel.RegTime = DateTime.Now;
            umbrModel.ModTime = DateTime.Now;
            umbrModel.FlagTrashed = false;
            umbrModel.FlagDeleted = false;
            umbrModel.RegUser = Guid.Parse(CFG.异议处理用户);
            //数据库马币记录添加
            umbrList.Add(umbrModel);
            //Json马币数据添加
            umbrListJson.Add(new bmUserMaBiRecordJson
            {
                ID = umbrModel.ID,
                SourceRef = umbrModel.SourceRef,
                MaBiRef = umbrModel.MaBiRef,
                MaBiNum = umbrModel.MaBiNum,
                QAId = umbrModel.QAId,
                DisId = umbrModel.DisId,
                OBId = umbrModel.OBId,
                UserId = umbrModel.UserId,

                IsSettle = umbrModel.IsSettle,
                RegUser = umbrModel.RegUser,
                RegTime = umbrModel.RegTime,
                ModTime = umbrModel.ModTime,
                FlagTrashed = umbrModel.FlagTrashed,
                FlagDeleted = umbrModel.FlagDeleted
            });
        }

        /// <summary>
        /// 异议处理通知用户
        /// </summary>
        /// <param name="mrbll"></param>
        /// <param name="email"></param>
        /// <param name="nickName"></param>
        /// <param name="qaId"></param>
        /// <param name="qaNum"></param>
        private void OBQAMail(BaseBll<wmfMailRecord> mrbll, string email, string nickName, string qaId, string qaNum)
        {
            LogHelper.Write(email + "发送邮件", LogHelper.LogMessageType.Debug);
            string fromEmail = CFG.应用邮箱;
            string fromEmailPassword = CFG.邮箱密码.DP();
            int emailPort = String.IsNullOrEmpty(CFG.邮箱端口) ? 587 : CFG.邮箱端口.ToAs<int>();

            string body = new WebClient().GetHtml("ServiceDomain".GHU() + "/Home/Q/" + qaId);
            //创建邮件对象并发送
            var mail = new SendMail(email, fromEmail, body, "异议处理结果通知 问题编号：" + qaNum, fromEmailPassword, "ServiceMailName".GX(), nickName);
            var mailRecord = new wmfMailRecord().wmfMailRecord2(email, body, "异议处理结果通知", "ServiceMailName".GX(), nickName, Guid.Parse(Reference.电子邮件类别_异议处理结果通知));
            mrbll.Insert(mailRecord);
            mail.Send("smtp.", emailPort, email + "异议处理结果通知邮件发送失败！");
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
