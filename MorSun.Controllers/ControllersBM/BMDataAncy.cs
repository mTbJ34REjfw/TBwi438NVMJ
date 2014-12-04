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
                    var result = "";
                    var dts = DateTime.Now.ToString();//dt.ToShortDateString() + " " + dt.ToShortTimeString();
                    var tok = HttpUtility.UrlEncode(SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码));
                    string strUrl = CFG.网站域名 + CFG.数据同步_用户信息 + "?tok=" + tok;
                    if (SyncDT != null)
                        strUrl += "&SyncDT=" + SyncDT;
                    LogHelper.Write("同步用户信息" + strUrl, LogHelper.LogMessageType.Info);
                    result = GetHtmlHelper.GetPage(strUrl, "");
                    if(!String.IsNullOrEmpty(result))
                    {
                        var s = DecodeJson(result);
                        //用户有三张表，要先分开
                        var aspUS = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔));
                        s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                        var aspMB = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔));
                        s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                        var wmfUI = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔));

                        var uids = new List<Guid>();
                        //数据处理
                        if(!String.IsNullOrEmpty(aspUS))
                        {
                            aspUS = Compression.DecompressString(aspUS);
                            var _list = JsonConvert.DeserializeObject<List<aspnet_Users>>(aspUS);
                            if(_list.Count() > 0)
                            {
                                uids = _list.Select(p => p.UserId).ToList();
                                var bll = new BaseBll<aspnet_Users>();
                                //过滤掉已经添加的数据
                                var alreadyUIds = bll.All.Where(p => uids.Contains(p.UserId)).Select(p => p.UserId);
                                uids = uids.Except(alreadyUIds).ToList();
                                _list = _list.Where(p => uids.Contains(p.UserId)).ToList();
                                foreach(var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
                            }
                        }

                        if (!String.IsNullOrEmpty(aspMB))
                        {
                            aspMB = Compression.DecompressString(aspMB);
                            var _list = JsonConvert.DeserializeObject<List<aspnet_Membership>>(aspMB);
                            if (_list.Count() > 0)
                            {
                                var bll = new BaseBll<aspnet_Membership>();
                                //过滤掉已经添加的数据                                                       
                                _list = _list.Where(p => uids.Contains(p.UserId)).ToList();
                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
                            }
                        }

                        if (!String.IsNullOrEmpty(wmfUI))
                        {
                            wmfUI = Compression.DecompressString(wmfUI);
                            var _list = JsonConvert.DeserializeObject<List<wmfUserInfo>>(wmfUI);
                            if (_list.Count() > 0)
                            {

                                var bll = new BaseBll<wmfUserInfo>();
                                //过滤掉已经添加的数据                                
                                _list = _list.Where(p => uids.Contains(p.ID)).ToList();
                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
                            }
                        }
                    }
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
