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
using Newtonsoft.Json;
using HOHO18.Common.DEncrypt;
using HOHO18.Common.Helper;

namespace MorSun.Controllers.SystemController
{
    public class BMTakeNowController : BaseController<bmTakeNow>
    {
        protected override string ResourceId
        {
            get { return MorSun.Common.Privelege.资源.取现; }
        }
        
        /// <summary>
        /// 取现数据判断有效性并生成马币记录和同步
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AncyTakeNow(string returnUrl)
        {
            var tkList = Bll.All.Where(p => p.Effective == null && p.TakeRef == null);
            var userids = tkList.Select(p => p.UserId).ToList();
            //取出所有用户马币值
            var userMB = GetUserMaBiByUIds(userids);
            //将用户马币放进List 可增减
            var userMBList = new List<bmUserMBJson>();
            foreach(var m in userMB)
            {
                userMBList.Add(new bmUserMBJson { UserId = m.UserId, UserMB = m.NMB });
            }
            //设置取现记录是否可取现
            var _canTakeList = new List<bmCanTakeNowJson>();
            foreach(var tk in tkList)
            {//本地的用户马币充足
                var localCanTake = false;
                if(tk.UserId == null || tk.MaBiNum <= 0)
                {
                    localCanTake = false;
                }
                else
                { 
                    var thisUMB = userMBList.FirstOrDefault(p => p.UserId == tk.UserId);
                    if(thisUMB.UserMB >= tk.MaBiNum)
                    {
                        thisUMB.UserMB -= tk.MaBiNum;
                        localCanTake = true;
                    }
                    else
                    {
                        localCanTake = false;
                    }
                }
                //判断服务器的该用户马币是否充足                
                _canTakeList.Add(new bmCanTakeNowJson{ID = tk.ID,LocalCanTake = localCanTake});      
            }

            var s = "";
            var result = "";
            if (_canTakeList.Count() == 0)
            {
                s += " ";
            }
            else
            {                
                s += ToJsonAndCompress(_canTakeList);
                s += CFG.邦马网_JSON数据间隔;

                //向邦马网同步马币充值数据                
                var dts = DateTime.Now.ToString();
                var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                var strUrl = CFG.网站域名 + CFG.数据同步_是否能取现;

                //未Encode的URL
                string neAppentUrl = "?tok=" + tok;
                if (!string.IsNullOrEmpty(s))
                {
                    neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                }

                LogHelper.Write("检测是否能取现" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                //有传递UID时用POST方法，参数有可能会超过URL长度
                result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
            }

            var oper = new OperationResult(OperationResultType.Error, "提交失败");
            if (_canTakeList.Count() == 0)
            {
                fillOperationResult(returnUrl, oper, "无同步的取现数据");
                return Json(oper, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //将获取下来的取现记录进行生成马币以及设置为有效或无效的操作
                if (!String.IsNullOrEmpty(result))
                {
                    LogHelper.Write("取现记录已经过本地与服务器的验证", LogHelper.LogMessageType.Info);
                    var bs = "";
                    try { bs = DecodeJson(result); }
                    catch
                    {
                        bs = "";
                        LogHelper.Write("解密异常", LogHelper.LogMessageType.Info);
                    }

                    if (!String.IsNullOrEmpty(bs))
                    {
                        //获取本地与服务器都验证的取现数据
                        var bmCTs = bs.Substring(0, bs.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                        bs = bs.Substring(bs.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                        //能保存在本地数据库的说明用户ID已经同步
                        try
                        {
                            //卡密充值记录
                            if (!String.IsNullOrEmpty(bmCTs))
                            {
                                bmCTs = Compression.DecompressString(bmCTs);
                                var _list = JsonConvert.DeserializeObject<List<bmCanTakeNowJson>>(bmCTs);
                                if (_list.Count() > 0)
                                {
                                    //本地与服务器都验证可以取现的取现记录ID
                                    var canTKNIds = _list.Where(p => p.LocalCanTake == true && p.ServerCanTake == true).Select(p => p.ID).ToList();
                                    var noTKNIds = _list.Where(p => !canTKNIds.Contains(p.ID)).Select(p => p.ID).ToList();
                                    //有效的取现记录，本地标识为有效，并生成取现马币记录。无效的本地标识为无效。
                                    var umbrBll = new BaseBll<bmUserMaBiRecord>();
                                    var umbrList = new List<bmUserMaBiRecord>();

                                    //有效的取现记录json 无效的取现记录json 添加的马币json
                                    var canTKNListJson = new List<bmCanTakeNowJson>();
                                    var noTKNListJson = new List<bmCanTakeNowJson>();
                                    var umbrListJson = new List<bmUserMaBiRecordJson>();

                                    //数据库的取现数据
                                    var canTKNList = tkList.Where(p => canTKNIds.Contains(p.ID));
                                    var noTKNList = tkList.Where(p => noTKNIds.Contains(p.ID));

                                    var yx = Guid.Parse(Reference.卡密有效性_有效);
                                    var qxwx = Guid.Parse(Reference.卡密有效性_无效);
                                    var wqx = Guid.Parse(Reference.取现情况_未取);
                                    var mbRef = Guid.Parse(Reference.马币类别_马币);
                                    //取现的马币记录是负数
                                    if (canTKNList.Count() > 0)
                                    {
                                        foreach (var ct in canTKNList)
                                        {
                                            //有效取现记录，生成并添加到json集合
                                            ct.Effective = yx;
                                            ct.TakeRef = wqx;
                                            //可取现的json添加
                                            canTKNListJson.Add(new bmCanTakeNowJson { ID = ct.ID,Effective = ct.Effective,TakeRef = ct.TakeRef});

                                            //生成马币取现记录
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
                                        }
                                    }
                                    if (noTKNList.Count() > 0)
                                    {
                                        foreach (var nct in noTKNList)
                                        {
                                            nct.Effective = qxwx;

                                            noTKNListJson.Add(new bmCanTakeNowJson { ID = nct.ID, Effective = nct.Effective });
                                        }
                                    }

                                    //有效的取现记录发送到服务器去生成马币记录与修改取现记录。canTKNListJson，umbrListJson
                                    //无效的取现记录发送到服务器去修改取现记录noTKNListJson
                                    var yzs = "";
                                    var yzresult = "";
                                    if (canTKNList.Count() == 0)
                                    {
                                        yzs += " ";
                                    }
                                    else
                                    {
                                        yzs += ToJsonAndCompress(canTKNListJson);
                                    }
                                    yzs += CFG.邦马网_JSON数据间隔;

                                    if(umbrList.Count() == 0)
                                    {
                                        yzs += " ";
                                    }
                                    else
                                    {
                                        yzs += ToJsonAndCompress(umbrListJson);
                                    }
                                    yzs += CFG.邦马网_JSON数据间隔;

                                    if (noTKNList.Count() == 0)
                                    {
                                        yzs += " ";
                                    }
                                    else
                                    {
                                        yzs += ToJsonAndCompress(noTKNListJson);
                                    }
                                    yzs += CFG.邦马网_JSON数据间隔;

                                    if(!String.IsNullOrEmpty(yzs))
                                    {
                                        //向邦马网同步马币充值数据                
                                        var dts = DateTime.Now.ToString();
                                        var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                                        var strUrl = CFG.网站域名 + CFG.数据同步_取现马币;

                                        //未Encode的URL
                                        string neAppentUrl = "?tok=" + tok;
                                        if (!string.IsNullOrEmpty(yzs))
                                        {
                                            neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(yzs);
                                        }

                                        LogHelper.Write("检测是否能取现" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                                        //有传递UID时用POST方法，参数有可能会超过URL长度
                                        yzresult = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                                    }

                                    if(yzresult == "true")
                                    {
                                        //有效与无效的取现同步成功后，本地数据库修改取现数据，以及添加取现马币记录
                                        Bll.UpdateChanges();
                                        foreach (var m in umbrList)
                                        {
                                            umbrBll.Insert(m, false);
                                        }
                                        umbrBll.UpdateChanges();
                                        fillOperationResult(returnUrl, oper, "成功验证取现数据的有效性");
                                        return Json(oper, JsonRequestBehavior.AllowGet);
                                    }   
                                    else
                                    {
                                        fillOperationResult(returnUrl, oper, "服务器同步取现数据出错");
                                        return Json(oper, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            LogHelper.Write("验证数据解密异常", LogHelper.LogMessageType.Info);
                        }
                        fillOperationResult(returnUrl, oper, "验证数据解密异常");
                        return Json(oper, JsonRequestBehavior.AllowGet);
                    }//!String.IsNullOrEmpty(bs)
                    else
                    {
                        fillOperationResult(returnUrl, oper, "解密同步后的数据为空");
                        return Json(oper, JsonRequestBehavior.AllowGet);
                    }
                }//ifresult
                else
                {
                    fillOperationResult(returnUrl, oper, "无返回数据");
                    return Json(oper, JsonRequestBehavior.AllowGet);
                }
            }//_cattkList.Count           
        }

        /// <summary>
        /// 有效的取现数据，生成取现金额。注意，一周多次取现的情况
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateMoney(string returnUrl)
        {
            var yx = Guid.Parse(Reference.卡密有效性_有效);
            var wqx = Guid.Parse(Reference.取现情况_未取);
            var TWStartT = Convert.ToDateTime(TimeFormatHelper.ChinaWeekFirstDay());
            var TWEndT = Convert.ToDateTime(TimeFormatHelper.ChinaWeekLastDay()).AddDays(1).AddSeconds(-1);
            //本周内的取现记录
            var thisWeekTK = Bll.All.Where(p => p.RegTime >= TWStartT && p.RegTime <= TWEndT).OrderBy(p => p.RegTime);
            //还未取现的记录
            var tkList = Bll.All.Where(p => p.Effective == yx && (p.TakeRef == null || p.TakeRef == wqx));
            foreach(var tk in tkList)
            {
                var thisUTk = thisWeekTK.Where(p => p.UserId == tk.UserId).OrderBy(p => p.RegTime);
                if(thisUTk.Count() > 1)
                {
                    if(thisUTk.FirstOrDefault().ID != tk.ID)
                    {
                        tk.TakeMoney = (tk.MaBiNum.Value / 1000) * Convert.ToDecimal(0.99);
                        tk.BMExplain = "取现金额扣取了1%的手续费";
                    }
                    else
                    {
                        tk.TakeMoney = tk.MaBiNum.Value / 1000;
                        tk.BMExplain = "足额取现";
                    }
                }
                else
                {
                    tk.TakeMoney = tk.MaBiNum.Value / 1000;
                    tk.BMExplain = "足额取现";
                }
            }
            Bll.UpdateChanges();

            var oper = new OperationResult(OperationResultType.Error, "提交失败");
            fillOperationResult(returnUrl, oper, "取现金额生成成功");
            return Json(oper, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 取现
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        
        public ActionResult TakeMoney(bmTakeNow t, string returnUrl)
        {
            var oper = new OperationResult(OperationResultType.Error, "提交失败");
            var model = Bll.GetModel(t);
            var yx = Guid.Parse(Reference.卡密有效性_有效);
            var wqx = Guid.Parse(Reference.取现情况_未取);
            var yqx = Guid.Parse(Reference.取现情况_已取);
            if(model.Effective != yx || model.TakeRef != wqx || model.TakeMoney == null || model.TakeMoney == 0)
            {
                fillOperationResult(returnUrl, oper, "不符合取现条件");
                return Json(oper, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.TakeRef = yqx;
                model.TakeTime = DateTime.Now;

                var s = "";
                var result = "";
                var tnModel = new bmTakeNowJson 
                { 
                    ID=model.ID,
                    TakeRef = model.TakeRef, 
                    TakeMoney = model.TakeMoney, 
                    TakeTime = model.TakeTime,
                    BMExplain = model.BMExplain
                };
                s += ToJsonAndCompress(tnModel);
                s += CFG.邦马网_JSON数据间隔;

                //向邦马网同步马币充值数据                
                var dts = DateTime.Now.ToString();
                var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                var strUrl = CFG.网站域名 + CFG.数据同步_取现同步;

                //未Encode的URL
                string neAppentUrl = "?tok=" + tok;
                if (!string.IsNullOrEmpty(s))
                {
                    neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                }

                LogHelper.Write("取现" + strUrl + neAppentUrl + "ID:" + model.ID, LogHelper.LogMessageType.Info);
                //有传递UID时用POST方法，参数有可能会超过URL长度
                result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                if(result == "true")
                {
                    Bll.Update(model);
                    //发送邮件
                    var user = model.aspnet_Users1;
                    if(user != null)
                    { 
                        SendTakeNowMail(new BaseBll<wmfMailRecord>(), user.UserName, user.wmfUserInfo.NickName, model.MaBiNum.ToString("f0"), model.TakeMoney.ToString("f2"));
                    }
                    fillOperationResult(returnUrl, oper, "取现金额生成成功");
                    return Json(oper, JsonRequestBehavior.AllowGet);
                }
                else
                { 
                    fillOperationResult(returnUrl, oper, "取现同步到服务器时失败");
                    return Json(oper, JsonRequestBehavior.AllowGet);
                }
            }
        }

        /// <summary>
        /// 取现成功发送Email
        /// </summary>
        /// <param name="mrbll"></param>
        /// <param name="email"></param>
        /// <param name="nickName"></param>
        /// <param name="userNameString"></param>
        private void SendTakeNowMail(BaseBll<wmfMailRecord> mrbll, string email, string nickName, string takeMB, string takeMoney)
        {
            LogHelper.Write(email + "发送邮件", LogHelper.LogMessageType.Debug);
            string fromEmail = CFG.应用邮箱;
            string fromEmailPassword = CFG.邮箱密码.DP();
            int emailPort = String.IsNullOrEmpty(CFG.邮箱端口) ? 587 : CFG.邮箱端口.ToAs<int>();

            string body = new WebClient().GetHtml("ServiceDomain".GHU() + "/Home/SendTakeNowEmail").Replace("[==NickName==]", nickName).Replace("[==TakeMB==]", takeMB).Replace("[==TakeMoney==]", takeMoney);
            //创建邮件对象并发送
            var mail = new SendMail(email, fromEmail, body, "取现通知", fromEmailPassword, "ServiceMailName".GX(), nickName);
            var mailRecord = new wmfMailRecord().wmfMailRecord2(email, body, "取现通知", "ServiceMailName".GX(), nickName, Guid.Parse(Reference.电子邮件类别_取现通知));
            mrbll.Insert(mailRecord);
            mail.Send("smtp.", emailPort, email + "取现通知邮件发送失败！");
        }

        protected override string OnAddCK(bmTakeNow t)
        {              
            return "";
        }

        protected override string OnEditCK(bmTakeNow t)
        {                       
            return "";
        }        
    }
}
