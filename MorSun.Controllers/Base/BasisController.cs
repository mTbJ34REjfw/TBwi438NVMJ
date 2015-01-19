using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using HOHO18.Common;
using MorSun.Bll;
using MorSun.Controllers.Filter;
using MorSun.Model;
using MorSun.Controllers.ViewModel;
using System.Reflection;
using FastReflectionLib;
using System.Web.Script.Serialization;
using MorSun.Common.Privelege;
using System.Data.Objects.DataClasses;
using MorSun.Controllers;
using HOHO18.Common.Web;
using MorSun.Common.类别;
using MorSun.Common.配置;
using HOHO18.Common.SSO;
using HOHO18.Common.WEB;
using HOHO18.Common.DEncrypt;
using Newtonsoft.Json;

namespace System
{
    public static class ControllerHelper
    {
        /// <summary>
        /// 判断是否有权限
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        public static bool HP(this string resourceId, string operationId)
        {
            return MorSun.Controllers.BasisController.havePrivilege(resourceId, operationId);
        }

        /// <summary>
        /// 根据GUID字符串返回类别名称
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string getRef(this string str)
        {
            return BasisController.GetTypeName(str);
        }

        public static string getBC(this string str,string css)
        {
            return BasisController.GBSC(str,css);
        }        
    }
}

namespace MorSun.Controllers
{    
    public class BasisController : Controller
    {   
        #region 返回对象处理
        /// <summary>
        /// 返回的错误对象封装
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="oper"></param>
        /// <param name="defAction"></param>
        /// <param name="defController"></param>
        protected void fillOperationResult(string returnUrl, OperationResult oper, string message = "操作成功", string defAction = "index", string defController = "home")
        {
            oper.ResultType = OperationResultType.Success;
            oper.Message = message;
            oper.AppendData = string.IsNullOrEmpty(returnUrl) ? Url.Action(defAction, defController) : returnUrl;
        }
        /// <summary>
        /// SSO登录时使用，需要访问子网站
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="SSOLink"></param>
        /// <param name="oper"></param>
        /// <param name="message"></param>
        /// <param name="defAction"></param>
        /// <param name="defController"></param>
        protected void fillOperationResult(string returnUrl, string SSOLink, OperationResult oper, string message = "操作成功", string defAction = "index", string defController = "home")
        {
            oper.ResultType = OperationResultType.Success;
            oper.Message = message;
            oper.AppendData = string.IsNullOrEmpty(returnUrl) ? Url.Action(defAction, defController) : returnUrl;
            oper.SSOLink = SSOLink;
        }
        #endregion  

        #region 基本信息
        /// <summary>
        /// 用户ID
        /// </summary>        
        protected static Guid UserID
        {
            get
            {
                string name = System.Web.HttpContext.Current.User.Identity.Name;
                if (string.IsNullOrEmpty(name))
                    System.Web.HttpContext.Current.Response.Redirect(FormsAuthentication.LoginUrl);
                MembershipUser user = Membership.GetUser();
                if (user == null)
                    System.Web.HttpContext.Current.Response.Redirect(FormsAuthentication.LoginUrl);
                return new Guid(user.ProviderUserKey.ToString());
            }
        }     

        /// <summary>
        /// 当前用户基本信息
        /// </summary>        
        public static aspnet_Users CurrentAspNetUser
        {
            get
            {
                aspnet_Users user = new BaseBll<aspnet_Users>().GetModel(UserID);
                return user;
            }
        }

        public static bmNewUserMB CurrentUserMabi
        {
            get
            {
                return GetUserMaBiByUId(UserID);
            }
        }

        /// <summary>
        /// 根据用户ID取各种马币值
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        protected static bmNewUserMB GetUserMaBiByUId(Guid userId)
        {
            //取出当前用户剩余的马币值(包括未结算)
            return new BaseBll<bmNewUserMB>().All.FirstOrDefault(p => p.UserId == userId);            
        }

        /// <summary>
        /// 传入用户ID集，检测是否能取现
        /// </summary>
        /// <param name="userIds"></param>
        /// <returns></returns>
        protected static IQueryable<bmNewUserMB> GetUserMaBiByUIds(List<Guid?> userIds)
        {
            var uids = new List<Guid>();
            foreach(var l in userIds)
            {
                if(l != null)
                {
                    uids.Add(l.Value);
                }
            }
            //取出当前用户剩余的马币值(包括未结算)
            return new BaseBll<bmNewUserMB>().All.Where(p => uids.Contains(p.UserId));
        }
        #endregion

        /// <summary>
        /// 特殊超级用户
        /// </summary>
        /// <returns></returns>
        private static bool IsAU()
        {
            return UserID.ToString().Eql("FBE691B1B0C15342C9EA792B733369408D118C60F094D9440E00E4BD04B3E073B48504DB1A5F810A".DP());// ("AU".GX().DP());防止被串改，不从XML获取
        }
       
        #region 认证访问操作
        /// <summary>
        /// 是否为认证访问
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="rz"></param>
        /// <returns></returns>
        protected static bool IsRZ(string tok, bool rz, HttpRequestBase rq)
        {
            try
            {
                //判断是否是正常渠道访问
                var ts = SecurityHelper.Decrypt(tok);
                //取时间戳
                var ind = ts.IndexOf(';');
                DateTime dt = DateTime.Parse(ts.Substring(0, ind));
                //用定时器执行时会延迟，5秒不够
                if (dt.AddSeconds(12) < DateTime.Now || !ts.Contains(CFG.邦马网_对接统一码))
                {//限制8秒内
                    rz = false;
                    LogHelper.Write("访问未认证" + rq.RawUrl, LogHelper.LogMessageType.Info);
                }
                else
                {
                    rz = true;
                    LogHelper.Write("时间：" + ts.Substring(0, ind), LogHelper.LogMessageType.Debug);
                }
            }
            catch
            {
                rz = false;
                LogHelper.Write("访问各种原因认证出错" + rq.RawUrl, LogHelper.LogMessageType.Error);
            }
            return rz;
        }

        /// <summary>
        /// 对象序列化为JSON并压缩
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        protected static string ToJsonAndCompress(object v)
        {
            var jsonV = JsonConvert.SerializeObject(v);
            return Compression.CompressString(jsonV);
        }

        /// <summary>
        /// 对JSON进行加密
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected static string EncodeJson(string s)
        {
            //var ys = Compression.CompressString(s);
            var dts = DateTime.Now.ToString();
            var eys = SecurityHelper.Encrypt(dts + ";" + s);
            return eys;
        }
        /// <summary>
        /// 对JSON进行解密
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected static string DecodeJson(string id)
        {
            try
            {
                var eys = SecurityHelper.Decrypt(id);
                var ys = eys.Substring(eys.IndexOf(';') + 1);
                //var s = Compression.DecompressString(ys);
                return ys;
            }
            catch
            {
                return "";
            }
        }
        #endregion
        

        #region 权限
        public static List<wmfRolePrivilegesView> getSessionPrivileges()
        {
            if (System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] == null)
                setSessionPrivileges();
            if (String.Compare("无权限", System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] as string) == 0)
                return null;
            else
                return System.Web.HttpContext.Current.Session["SessionPrivilege"] as List<wmfRolePrivilegesView>;
        }

        /// <summary>
        /// 判断当前用户是否含有对某资源的访问权限
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        public static bool havePrivilege(string resourceId, string operationId)
        {
            if (IsAU())
                return true;
            if (BasisController.getSessionPrivileges() == null)
                return false;
            else
            {
                return BasisController.getSessionPrivileges().Any(p => string.Compare(p.OperationId, operationId, true) == 0
                    && string.Compare(p.ResourceId, resourceId, true) == 0);
            }
        }

        /// <summary>
        /// 判断当前用户是否含有对某资源的访问权限,多个操作判断，关系为或，只要一个为真，返回真
        /// add by timfeng 213-8-26
        /// </summary>
        public static bool havePrivilege(string resourceId, params string[] operationIds)
        {
            var res = false;
            if (BasisController.getSessionPrivileges() == null)
                return false;
            foreach (var operateId in operationIds)
            {
                res = BasisController.getSessionPrivileges().Any(p => string.Compare(p.OperationId, operateId, true) == 0
                    && string.Compare(p.ResourceId, resourceId, true) == 0);
                if (res)
                    return res;
            }
            return res;
        }

        /// <summary>
        /// 判断当前用户是否含有对某资源的访问权限,含有操作参数
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="operationId"></param>
        /// <param name="privilegeValue">权限参数</param>
        /// <returns></returns>
        public static bool havePrivilegeWithPrivilegeValue(string resourceId, string operationId, string privilegeValue)
        {
            if (BasisController.getSessionPrivileges() == null)
                return false;
            else
            {
                return BasisController.getSessionPrivileges().Any(p => string.Compare(p.OperationId, operationId, true) == 0
                    && string.Compare(p.ResourceId, resourceId, true) == 0);                
            }
        }        

        /// <summary>
        /// 从数据库中读取全下列表
        /// <param name="userid">当前用户UserID</param>
        /// </summary>
        public static IList<wmfRolePrivilegesView> getSessionPrivilegesByDatabase(Guid userid)
        {
            var currUser = new BaseBll<aspnet_Users>().GetModel(userid);
            //取出当前用户所有的权限-角色集合 这边方法不行，用上面的
            var rolesId = currUser.aspnet_Roles.SingleOrDefault().RoleId;/*currUser.aspnet_Roles.Select(r => r.RoleId).ToArray();*/
           
            var rolePrivilegeViewBll = new BaseBll<wmfRolePrivilegesView>();
            var currentUserPrivileges = rolePrivilegeViewBll.All.Where(u => u.RoleId == rolesId).ToList();
            return currentUserPrivileges;
        }

        /// <summary>
        /// 取出当前用户的权限列表，并放在Session中。
        /// </summary>
        public static void setSessionPrivileges()
        {
            var sessionPrivilegeList = getSessionPrivilegesByDatabase(UserID);
            if (sessionPrivilegeList.Any())
            {
                System.Web.HttpContext.Current.Session["SessionPrivilege"] = sessionPrivilegeList;
                System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] = "有权限";//有权限ID集
            }
            else
            {
                System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] = "无权限";//是没有权限ID集
            }
        }
        #endregion

        #region 插入日志
        /// <summary>
        /// 插入日志
        /// </summary>
        /// <param name="ID">修改记录ID</param>
        /// <param name="tableName">表名</param>
        /// <param name="operateContent">操作类型</param>
        /// <param name="originalContent">修改前</param>
        /// <param name="afterOperateContent">修改后</param>
        public void InsertLog(Guid? ID, string tableName, string operateContent, string originalContent, string afterOperateContent)
        {
            string result = string.Empty;
            var opeateBill = new BaseBll<wmfOperationalLogbook>();
            var model = new wmfOperationalLogbook();
            model.UserId = UserID;
            //model.ApplicationId = AppId;//应用程序ID
            model.RegTime = DateTime.Now;//添加时间
            model.LinkId = ID;//修改记录ID
            model.OperateTable = tableName;
            model.OriginalContent = originalContent;
            model.AfterOperateContent = afterOperateContent;
            model.UserIP = IPAddress;//用户IP地址
            model.OperateContent = operateContent;//操作类型
            opeateBill.Insert(model);
        }

        /// <summary>
        /// 插入日志
        /// </summary>
        /// <param name="ID">修改记录ID</param>
        /// <param name="tableName">表名</param>
        /// <param name="operateContent">操作类型</param>
        /// <param name="originalContent">修改前</param>
        /// <param name="afterOperateContent">修改后</param>
        /// <param name="userID">用户Guid</param>
        public void InsertLog(Guid? ID, string tableName, string operateContent, string originalContent, string afterOperateContent, Guid userID)
        {
            string result = string.Empty;
            var opeateBill = new BaseBll<wmfOperationalLogbook>();
            var model = new wmfOperationalLogbook();
            model.UserId = userID;
            //model.ApplicationId = AppId;//应用程序ID
            model.RegTime = DateTime.Now;//添加时间
            model.LinkId = ID;//修改记录ID
            model.OperateTable = tableName;
            model.OriginalContent = originalContent;
            model.AfterOperateContent = afterOperateContent;
            model.UserIP = IPAddress;//用户IP地址
            model.OperateContent = operateContent;//操作类型
            opeateBill.Insert(model);
        }


        /// <summary>
        /// 插入日志
        /// </summary>
        /// <param name="ID">修改记录ID</param>
        /// <param name="tableName">表名</param>
        /// <param name="operateContent">操作类型</param>
        /// <param name="originalContent">修改前</param>
        /// <param name="afterOperateContent">修改后</param>
        /// <param name="userID">用户Guid</param>
        /// <param name="businessId">业务id、较多用于操作多表的功能（如一个订单号里面包含很多的商品，那么该订单号就是businessId）</param>
        public void InsertLog(Guid? ID, string tableName, string operateContent, string originalContent, string afterOperateContent, Guid userID, Guid? businessId)
        {
            string result = string.Empty;
            var opeateBill = new BaseBll<wmfOperationalLogbook>();
            var model = new wmfOperationalLogbook();
            model.UserId = userID;
            //model.ApplicationId = AppId;//应用程序ID
            model.RegTime = DateTime.Now;//添加时间
            model.LinkId = ID;//修改记录ID
            model.OperateTable = tableName;
            model.OriginalContent = originalContent;
            model.AfterOperateContent = afterOperateContent;
            model.UserIP = IPAddress;//用户IP地址
            model.OperateContent = operateContent;//操作类型
            model.BusinessId = businessId;//业务id
            opeateBill.Insert(model);
        }

        /// <summary>
        /// 插入访问记录
        /// </summary>
        /// <param name="pageBrowse"></param>
        public void InsertVisitInfo(wmfPageBrowse pageBrowse)
        {
            var pageBll = new BaseBll<wmfPageBrowse>();
            var model = new wmfPageBrowse();
            model = pageBrowse;
            model.IP = IPAddress;
            model.UserId = UserID;
            //model.ApplicationId = AppId;
            model.FirstTime = DateTime.Now;
            pageBll.Insert(model);
        }        
        #endregion

        #region 获取用户登录IP
        /// <summary>
        /// 获取用登录IP
        /// </summary>
        public string IPAddress
        {
            get
            {
                string user_IP;
                if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
                {
                    user_IP = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
                }
                else
                {
                    user_IP = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
                }
                if (user_IP == null || user_IP == "")
                    user_IP = Request.UserHostAddress;
                return user_IP;
            }
        }

        #endregion       

        #region 通过Guid字符串获取名称
        /// <summary>
        /// 通过Guid字符串获取名称
        /// </summary>
        /// <param name="str">Guid字符串</param>
        /// <returns>类型名称</returns>
        public static string GetTypeName(string str)
        {
            var ret = string.Empty;
            if (!string.IsNullOrEmpty(str))
            {
                var newStr = str.Split(',');
                var refList = new ReferenceVModel().All.OrderBy(t => t.Sort);
                for (int i = 0; i < newStr.Length; i++)
                {
                    if (newStr[i] != "")
                    {
                        var ID = Guid.Parse(newStr[i]);
                        ret += refList.Where(p => p.ID == ID).FirstOrDefault().ItemValue + ",";
                    }
                }
                ret = ret.TrimEnd(',');
            }
            return ret;
        }

        /// <summary>
        /// 获取Ref对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static wmfReference GetRefModel(Guid? id)
        {
            return new BaseBll<wmfReference>().GetModel(id);
        }
        #endregion

        #region 验证码判断方法
        /// <summary>
        /// 验证码验证
        /// </summary>
        /// <param name="model"></param>
        protected void validateVerifyCode(string verifyCode, string verifycodeRandom, string xmlconfigName)
        {
            //判断是否验证码开启
            if (xmlconfigName.GX() == "true")
            {
                //判断验证码是否填写
                if (String.IsNullOrEmpty(verifyCode))
                {
                    "Verifycode".AE("请填写验证码", ModelState);
                }
                if (VerifyCode.GetValue(verifycodeRandom) != null)
                {
                    object vCodeVal = VerifyCode.GetValue(verifycodeRandom);
                    if (String.IsNullOrEmpty(verifyCode) || vCodeVal == null || String.Compare(verifyCode, vCodeVal.ToString()) != 0)
                    {
                        "Verifycode".AE("验证码填错", ModelState);
                    }
                    else
                    {
                        //ajax的方式登录，要等登录成功之后才清除验证码数据
                    }
                }
                else
                {
                    "Verifycode".AE("验证码填错", ModelState);
                }
                //清除验证码信息
                clearVerifyCode(verifycodeRandom);
            }
        }

        /// <summary>
        /// 获取验证码类型
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected void clearVerifyCode(string verifycodeRandom)
        {
            HOHO18.Common.Web.VerifyCodeType type = HOHO18.Common.Web.VerifyCodeType.Login;
            try
            {
                string typeStr = verifycodeRandom;
                if (String.IsNullOrEmpty(typeStr))
                    type = HOHO18.Common.Web.VerifyCodeType.Login;
                else
                    type = (HOHO18.Common.Web.VerifyCodeType)Enum.Parse(typeof(HOHO18.Common.Web.VerifyCodeType), typeStr, true);
            }
            catch { }
            VerifyCode.RemoveValue(type);
        }
        #endregion

        #region 生成加密串
        protected string GenerateEncryptCode(string userNameString,string encryptUrl,bool updateChange = false)
        {
            var er = new wmfEncryptRecord();
            er.ID = Guid.NewGuid();
            er.UserNameString = userNameString;
            er.EncryptCode = Guid.NewGuid().ToString().EP(Guid.NewGuid().ToString());
            er.EncryptTime = DateTime.Now;
            er.EncryptUrl = encryptUrl;
            er.FlagTrashed = false;
            er.FlagDeleted = false;
            new BaseBll<wmfEncryptRecord>().Insert(er, updateChange);
            return er.EncryptCode;
        }
        #endregion

        #region 随机生成样式
        public static string GBSC(string str,string css)
        {
            var s = css.GX().Split(',');
            if (s.Count() > 0)
            {
                var i = s.Count();
                Random rd = new Random();
                int j = rd.Next(0,i);
                return s[j];
            }
            else
                return str;
        }
        #endregion

        #region 用户币记录
        /// <summary>
        /// 添加用户币记录  这个方法没有问题ID
        /// </summary>
        /// <param name="uIds">用户ID集，可批量添加。</param>
        /// <param name="sr">来源</param>
        /// <param name="mbr">币种</param>
        /// <param name="mbn">币值</param>
        public void AddUMBR(AddMBRModel addMBR, bool updateChange = true)
        {
            var rbll = new BaseBll<bmUserMaBiRecord>();  
            //检测用户是否存在
            var users = new BaseBll<aspnet_Users>().All.Where(p => addMBR.UIds.Contains(p.UserId));//找得到userId 就添加
            foreach (var u in users)
            {
                var model = new bmUserMaBiRecord();
                model.SourceRef = addMBR.SR;
                model.MaBiRef = addMBR.MBR;
                model.MaBiNum = addMBR.MBN;
                model.IsSettle = false;

                model.RegTime = DateTime.Now;
                model.ModTime = DateTime.Now;
                model.FlagTrashed = false;
                model.FlagDeleted = false;

                model.ID = Guid.NewGuid();
                model.UserId = u.UserId;
                if (User != null && User.Identity.IsAuthenticated)
                    model.RegUser = UserID;
                else
                    model.RegUser = u.UserId;
                rbll.Insert(model, false);
            }
            if (updateChange)
                rbll.UpdateChanges();
        }

        /// <summary>
        /// 批量设置为已结算
        /// </summary>
        /// <param name="mbList"></param>
        protected void setUMBRSettle(IQueryable<bmUserMaBiRecord> mbList, bool updateChange = true)
        {
            var rbll = new BaseBll<bmUserMaBiRecord>();
            foreach(var m in mbList)
            {
                m.IsSettle = true;
                m.ModTime = DateTime.Now;
            }
            if (updateChange)
                rbll.UpdateChanges();
        }

        /// <summary>
        /// 马币即时统计 更新数据库版
        /// </summary>
        protected void SettleMaBi()
        {
            var rbll = new BaseBll<bmUserMaBiRecord>();
            var umbbll = new BaseBll<bmUserMaBi>();
            //用户币增减记录
            var nonSettleMBR = rbll.All.Where(p => p.IsSettle == false && p.UserId != null);            
            var nonSMGroup = nonSettleMBR.GroupBy(p => p.UserId);
            var userIds = nonSMGroup.Select(p => p.Key);
            //用户各种币
            var userMabi = umbbll.All.Where(p => userIds.Contains(p.UserId));
            //币种
            var mabi = Guid.Parse(Reference.马币类别_马币);
            var bbi = Guid.Parse(Reference.马币类别_邦币);
            var banbi = Guid.Parse(Reference.马币类别_绑币);

            foreach(var smg in nonSMGroup)
            {
                //加马币
                var mabisum = smg.Where(p => p.MaBiRef == mabi).Sum(p => p.MaBiNum);
                if(mabisum != 0)
                {
                    var thisUserMabi = userMabi.Where(p => p.UserId == smg.Key && p.MaBiRef == mabi).FirstOrDefault();
                    if(thisUserMabi == null)
                    {//系统还没有添加此马币的情况                        
                        var model = GenerateUserMaBi(mabi, mabisum, smg.Key);
                        umbbll.Insert(model, false);
                    }
                    else
                    {
                        thisUserMabi.MaBiNum += mabisum;
                        thisUserMabi.SettleTime = DateTime.Now;
                        thisUserMabi.ModTime = DateTime.Now;
                    }
                }
                //加邦币
                var bbisum = smg.Where(p => p.MaBiRef == bbi).Sum(p => p.MaBiNum);
                if (bbisum != 0)
                {
                    var thisUserMabi = userMabi.Where(p => p.UserId == smg.Key && p.MaBiRef == bbi).FirstOrDefault();
                    if (thisUserMabi == null)
                    {//系统还没有添加此马币的情况  
                        var model = GenerateUserMaBi(bbi, bbisum, smg.Key);
                        umbbll.Insert(model, false);
                    }
                    else
                    {
                        thisUserMabi.MaBiNum += bbisum;
                        thisUserMabi.SettleTime = DateTime.Now;
                        thisUserMabi.ModTime = DateTime.Now;
                    }
                }


                //加绑币
                var banbisum = smg.Where(p => p.MaBiRef == banbi).Sum(p => p.MaBiNum);
                if (banbisum != 0)
                {
                    var thisUserMabi = userMabi.Where(p => p.UserId == smg.Key && p.MaBiRef == banbi).FirstOrDefault();
                    if (thisUserMabi == null)
                    {//系统还没有添加此马币的情况                        
                        var model = GenerateUserMaBi(banbi, banbisum, smg.Key);
                        umbbll.Insert(model, false);
                    }
                    else
                    {
                        thisUserMabi.MaBiNum += banbisum;
                        thisUserMabi.SettleTime = DateTime.Now;
                        thisUserMabi.ModTime = DateTime.Now;
                    }
                }
            }

            //将用户币记录设置为已结算
            foreach(var item in nonSettleMBR)
            {
                item.IsSettle = true;
                item.ModTime = DateTime.Now;
            }

            rbll.UpdateChanges();
        }
        /// <summary>
        /// 根据传入的参数生成用户马币对象
        /// </summary>
        /// <param name="mabi"></param>
        /// <param name="mabisum"></param>
        /// <param name="mbUserId"></param>
        /// <returns></returns>
        private static bmUserMaBi GenerateUserMaBi(Guid mabi, decimal? mabisum, Guid? mbUserId)
        {
            var model = new bmUserMaBi();
            model.ID = Guid.NewGuid();
            model.UserId = mbUserId;
            model.MaBiRef = mabi;
            model.MaBiNum = mabisum;
            model.SettleTime = DateTime.Now;

            model.RegUser = mbUserId;
            model.RegTime = DateTime.Now;
            model.ModTime = DateTime.Now;
            model.FlagTrashed = false;
            model.FlagDeleted = false;
            return model;
        }
        #endregion

        #region 获取用户的绑定信息
        /// <summary>
        /// 获取用户的微信绑定作业邦信息
        /// </summary>
        /// <returns></returns>
        protected bmUserWeixin GetUserBound()
        {
            var wxyy = Guid.Parse(Reference.微信应用_作业邦);
            return new BaseBll<bmUserWeixin>().All.FirstOrDefault(p => p.UserId == UserID && p.WeiXinAPP == wxyy);
        }

        /// <summary>
        /// 获取用户绑定信息
        /// </summary>
        /// <returns></returns>
        //protected UserBoundCache GetUserBoundCache()
        //{
        //    var key = CFG.微信绑定前缀 + UserID.ToString();
        //    var ubc = CacheAccess.GetFromCache(key) as UserBoundCache;
        //    //如果缓存为空，重新设置值
        //    if(ubc == null)
        //    {
        //        ubc = new UserBoundCache();
        //        ubc.UserId = UserID;
        //        Random Rdm = new Random();
        //        int iRdm = 0;
        //        do
        //        {
        //            iRdm = Rdm.Next(1, 999999);

        //        } while (GetUserBoundCodeCache(iRdm) != null);//不为空才会再生成
        //        //马上设置生成码缓存
        //        var codeKey = CFG.微信绑定前缀 + iRdm.ToString();
        //        var ubcc = new UserBoundCodeCache();
        //        ubcc.UserId = UserID;
        //        ubcc.BoundCode = iRdm;
        //        CacheAccess.InsertToCacheByTime(codeKey, ubcc, 120);//两分钟内过期
        //        ubc.BoundCode = iRdm;
        //        CacheAccess.InsertToCacheByTime(key, ubc, 120);
        //    }
        //    return ubc;
        //}

        /// <summary>
        /// 根据绑定代码取要绑定的用户
        /// </summary>
        /// <param name="boundCode"></param>
        /// <returns></returns>
        //protected UserBoundCodeCache GetUserBoundCodeCache(int boundCode)
        //{
        //    var key = CFG.微信绑定前缀 + boundCode.ToString();
        //    return CacheAccess.GetFromCache(key) as UserBoundCodeCache;
        //}
        #endregion

        #region 数据同步
        /// <summary>
        /// 同步用户
        /// </summary>
        /// <param name="SyncDT">同步开始时间</param>
        /// <param name="neURLuids">需要同步的用户ID：e26ef963-ff8d-4569-b019-7fe16103c934,1479a879-3427-40b0-a697-b7385ad9aa6d</param>
        public void AncyUser(DateTime? SyncDT, string neURLuids)
        {
            var result = "";
            var dts = DateTime.Now.ToString();          
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
            string strUrl = CFG.网站域名 + CFG.数据同步_用户信息;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);
            if (SyncDT != null)
                appendUrl += "&SyncDT=" + SyncDT;
            if (!string.IsNullOrEmpty(neURLuids))
            {                
                appendUrl += "&UIds=" + HttpUtility.UrlEncode(neURLuids);
            }

            //未Encode的URL
            string neAppentUrl = "?tok=" + tok;
            neAppentUrl += "&SyncDT=" + SyncDT;
            if (!string.IsNullOrEmpty(neURLuids))
            {
                neAppentUrl += "&UIds=" + SecurityHelper.Encrypt(neURLuids);
            }

            LogHelper.Write("同步用户信息" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            if (String.IsNullOrEmpty(neURLuids))// && SyncDT == null)
            {
                //当不传递UID时
                result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");
            }
            else
            {
                //有传递UID时用POST方法，参数有可能会超过URL长度
                result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
            }

            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("有获取到用户数据", LogHelper.LogMessageType.Info);
                var s = "";
                try { s = DecodeJson(result); }
                catch
                {
                    s = "";
                    LogHelper.Write("解密异常", LogHelper.LogMessageType.Info);
                }
                if (!String.IsNullOrEmpty(s))
                {
                    //用户有三张表，要先分开
                    var aspUS = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var aspMB = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var wmfUI = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();

                    var uids = new List<Guid>();
                    //数据处理
                    if (!String.IsNullOrEmpty(aspUS))
                    {                        
                        aspUS = Compression.DecompressString(aspUS);
                        var _list = JsonConvert.DeserializeObject<List<aspnet_Users>>(aspUS);
                        if (_list.Count() > 0)
                        {
                            uids = _list.Select(p => p.UserId).ToList();
                            var bll = new BaseBll<aspnet_Users>();
                            //过滤掉已经添加的数据
                            var alreadyUIds = bll.All.Where(p => uids.Contains(p.UserId)).Select(p => p.UserId);
                            uids = uids.Except(alreadyUIds).ToList();
                            _list = _list.Where(p => uids.Contains(p.UserId)).ToList();

                            foreach (var l in _list)
                            {
                                bll.Insert(l, false);
                            }
                            bll.UpdateChanges();

                            Guid rid = Guid.Parse(CFG.注册默认角色);
                            var role = new BaseBll<aspnet_Roles>().All.Where(p => p.RoleId == rid);
                            var constr = "";
                            if (role != null)
                            {
                                foreach (var l in _list)
                                {
                                    constr += @"Insert Into aspnet_UsersInRoles ([UserId],[RoleId])  VALUES ('" + l.UserId + "','" + CFG.注册默认角色 + "')";
                                }
                            }
                            if (!String.IsNullOrEmpty(constr))
                                bll.Db.ExecuteStoreCommand(constr);
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
            }
            else
            {
                LogHelper.Write("各种原因没有获取到用户数据", LogHelper.LogMessageType.Info);
            }
        }

        /// <summary>
        /// 问题、用户绑定数据同步
        /// </summary>
        /// <param name="SyncDT"></param>
        public void AncyQA(DateTime? SyncDT)
        {
            var result = "";
            var dts = DateTime.Now.ToString();          
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
            string strUrl = CFG.网站域名 + CFG.数据同步_作业邦信息;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);
            if (SyncDT != null)
                appendUrl += "&SyncDT=" + SyncDT;

            LogHelper.Write("同步问题信息" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");


            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("有获取到问题数据", LogHelper.LogMessageType.Info);
                var s = "";
                try { s = DecodeJson(result); }
                catch { s = "";
                LogHelper.Write("解密异常", LogHelper.LogMessageType.Info);
                }
                if (!String.IsNullOrEmpty(s))
                {
                    //用户有三张表，要先分开
                    var bmQA = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);                    
                    var qaDIS = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmOB = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmUW = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmTK = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmMB = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);

                    var ubll = new BaseBll<aspnet_Users>();
                    var uids = new List<Guid>();
                    var bmQAJson = "";                    
                    var qaDISJson = "";
                    var bmOBJson = "";
                    var bmUWJson = "";
                    var bmTKJson = "";
                    var bmMBJson = "";
                    //用户需要先同步
                    if (!String.IsNullOrEmpty(bmQA))
                    {//跟UserId没关系，就不同步UserId
                        bmQAJson = Compression.DecompressString(bmQA);
                        var _list = JsonConvert.DeserializeObject<List<bmQAJson>>(bmQAJson);
                        if (_list.Count() > 0)
                        {
                            var uid = _list.Where(p => p.UserId != null).Select(p => p.UserId.Value);
                            if (uid.Count() > 0)
                            {
                                foreach (var u in uid)
                                {
                                    uids.Add(u);
                                }
                            }
                        }
                    }                    

                    if (!String.IsNullOrEmpty(qaDIS))
                    {
                        qaDISJson = Compression.DecompressString(qaDIS);
                        var _list = JsonConvert.DeserializeObject<List<bmQADistributionJson>>(qaDISJson);
                        if (_list.Count() > 0)
                        {
                            var uid = _list.Where(p => p.UserId != null).Select(p => p.UserId.Value);
                            if (uid.Count() > 0)
                            {
                                foreach (var u in uid)
                                {
                                    uids.Add(u);
                                }
                            }
                        }
                    }

                    if (!String.IsNullOrEmpty(bmOB))
                    {
                        bmOBJson = Compression.DecompressString(bmOB);
                        var _list = JsonConvert.DeserializeObject<List<bmObjectionJson>>(bmOBJson);
                        if (_list.Count() > 0)
                        {
                            var uid = _list.Where(p => p.UserId != null).Select(p => p.UserId.Value);
                            if (uid.Count() > 0)
                            {
                                foreach (var u in uid)
                                {
                                    uids.Add(u);
                                }
                            }
                        }
                    }

                    if (!String.IsNullOrEmpty(bmUW))
                    {
                        bmUWJson = Compression.DecompressString(bmUW);
                        var _list = JsonConvert.DeserializeObject<List<bmUserWeixinJson>>(bmUWJson);
                        if (_list.Count() > 0)
                        {
                            var uid = _list.Where(p => p.UserId != null).Select(p => p.UserId.Value);
                            if (uid.Count() > 0)
                            {
                                foreach (var u in uid)
                                {
                                    uids.Add(u);
                                }
                            }
                        }
                    }

                    if (!String.IsNullOrEmpty(bmTK))
                    {//跟UserId没关系，就不同步UserId
                        bmTKJson = Compression.DecompressString(bmTK);
                        var _list = JsonConvert.DeserializeObject<List<bmTakeNow>>(bmTKJson);
                        if (_list.Count() > 0)
                        {
                            var uid = _list.Where(p => p.UserId != null).Select(p => p.UserId.Value);
                            if (uid.Count() > 0)
                            {
                                foreach (var u in uid)
                                {
                                    uids.Add(u);
                                }
                            }
                        }
                    }

                    if (!String.IsNullOrEmpty(bmMB))
                    {
                        bmMBJson = Compression.DecompressString(bmMB);
                        var _list = JsonConvert.DeserializeObject<List<bmUserMaBiRecordJson>>(bmMBJson);
                        if (_list.Count() > 0)
                        {
                            var uid = _list.Where(p => p.UserId != null).Select(p => p.UserId.Value);
                            if (uid.Count() > 0)
                            {
                                foreach (var u in uid)
                                {
                                    uids.Add(u);
                                }
                            }
                        }
                    }

                    //userids 去重复
                    uids = uids.Distinct().ToList();

                    //同步用户ID 下面的已经处理了哪里用户ID需要同步
                    if (uids.Count() > 0)
                        UIDAncyUser(ubll, uids);

                    var qabll = new BaseBll<bmQA>();
                    var qadisbll = new BaseBll<bmQADistribution>();
                    var uwbll = new BaseBll<bmUserWeixin>();
                    //同步过来的答题记录
                    var bmQAIDList = new List<Guid>();
                    //同步过来的异议处理记录
                    var bmQADisList = new List<Guid>();
                    //问题记录
                    try
                    {
                        if (!String.IsNullOrEmpty(bmQA))
                        {
                            //bmQA = Compression.DecompressString(bmQA);
                            var _list = JsonConvert.DeserializeObject<List<bmQA>>(bmQAJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                
                                //过滤掉已经添加的数据
                                var alreadyQIds = qabll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();

                                foreach (var l in _list)
                                {
                                    qabll.Insert(l, false);
                                }
                                qabll.UpdateChanges();
                            }
                        }
                        
                        //问题分配
                        if (!String.IsNullOrEmpty(qaDIS))
                        {
                            //qaDIS = Compression.DecompressString(qaDIS);
                            var _list = JsonConvert.DeserializeObject<List<bmQADistribution>>(qaDISJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                
                                //过滤掉已经添加的数据  
                                var alreadyQIds = qadisbll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();

                                foreach (var l in _list)
                                {
                                    qadisbll.Insert(l, false);
                                    if(l.QAId != null)
                                        bmQAIDList.Add(l.QAId.Value);
                                    bmQADisList.Add(l.ID);
                                }
                                qadisbll.UpdateChanges();
                                
                            }
                        }
                        //异议分配
                        if (!String.IsNullOrEmpty(bmOB))
                        {
                            //bmOB = Compression.DecompressString(bmOB);
                            var _list = JsonConvert.DeserializeObject<List<bmObjection>>(bmOBJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmObjection>();
                                //过滤掉已经添加的数据  
                                var alreadyQIds = bll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();

                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
                            }
                        }
                        //微信绑定
                        if (!String.IsNullOrEmpty(bmUW))
                        {
                            //bmUW = Compression.DecompressString(bmUW);
                            var _list = JsonConvert.DeserializeObject<List<bmUserWeixin>>(bmUWJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                
                                //过滤掉已经添加的数据  
                                var alreadyQIds = uwbll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();

                                foreach (var l in _list)
                                {
                                    uwbll.Insert(l, false);
                                }
                                uwbll.UpdateChanges();
                            }
                        }
                        
                        //取现
                        if (!String.IsNullOrEmpty(bmTK))
                        {
                            //bmUW = Compression.DecompressString(bmUW);
                            var _list = JsonConvert.DeserializeObject<List<bmTakeNow>>(bmTKJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmTakeNow>();
                                //过滤掉已经添加的数据  
                                var alreadyQIds = bll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();

                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
                            }
                        }

                        //马币记录
                        if (!String.IsNullOrEmpty(bmMB))
                        {
                            //bmMB = Compression.DecompressString(bmMB);
                            var _list = JsonConvert.DeserializeObject<List<bmUserMaBiRecord>>(bmMBJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmUserMaBiRecord>();
                                //过滤掉已经添加的数据                    
                                var alreadyQIds = bll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                //过滤时间超过现在1小时的数据

                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();
                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
                            }
                        }
                    }
                    catch
                    {
                        LogHelper.Write("用户数据获取异常导致同步问题时一些数据同步不成功", LogHelper.LogMessageType.Info);
                    }
                    //已经回答的问题发送邮件通知提问人员,测试发邮件通知时的效率，每发一封邮件之前都会将邮件内容保存进数据库
                    if(bmQAIDList.Count() >0)
                    {
                        bmQAIDList = bmQAIDList.Distinct().ToList();
                        var mrbll = new BaseBll<wmfMailRecord>();                    
                        var qaList = qabll.All.Where(p => bmQAIDList.Contains(p.ID));
                        var qaWeiXinIds = qaList.Select(p => p.WeiXinId);             
                        //提问用户
                        var userWeiXins = uwbll.All.Where(p => qaWeiXinIds.Contains(p.WeiXinId));

                        var disList = qadisbll.All.Where(p => bmQADisList.Contains(p.ID));
                        var qaDisWeiXinIds = disList.Select(p => p.WeiXinId);
                        var disWeiXins = uwbll.All.Where(p => qaDisWeiXinIds.Contains(p.WeiXinId));

                        foreach (var d in qaList)
                        {                            
                            try
                            {
                                var qaU = userWeiXins.FirstOrDefault(p => p.WeiXinId == d.WeiXinId).aspnet_Users1;
                                var tt = "您提交的问题已解答";
                                tt += d.AutoGrenteId.ToString();                               
                                JDQAMail(mrbll, qaU.UserName, qaU.wmfUserInfo.NickName, d.ID.ToString(), tt);                                
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Write("问题解答发邮件不成功 解答记录ID：" + d.ID + " 错误信息" + ex.Message, LogHelper.LogMessageType.Info);
                            }
                        }
                        foreach(var d in disList)
                        {
                            try
                            {
                                var tt = "您已解答了问题";
                                tt += d.bmQA.AutoGrenteId.ToString();                                
                                var disU = disWeiXins.FirstOrDefault(p => p.WeiXinId == d.WeiXinId).aspnet_Users1;
                                JDQAMail(mrbll, disU.UserName, disU.wmfUserInfo.NickName, d.bmQA.ID.ToString(), tt);  
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Write("问题解答发邮件不成功 解答记录ID：" + d.ID + " 错误信息" + ex.Message, LogHelper.LogMessageType.Info);
                            }
                        }
                        //mrbll.UpdateChanges();//这边保存发送邮件会出错，就不保存了
                    }
                }                
                else
                {
                    LogHelper.Write("各种原因没有获取到问题数据", LogHelper.LogMessageType.Info);
                }            
            }            
        }

        /// <summary>
        /// 问题解答后通知提问人员
        /// </summary>
        /// <param name="mrbll"></param>
        /// <param name="email"></param>
        /// <param name="nickName"></param>
        /// <param name="takeMB"></param>
        /// <param name="takeMoney"></param>
        public static void JDQAMail(BaseBll<wmfMailRecord> mrbll, string email, string nickName, string qaId, string mailTitle)
        {
            LogHelper.Write(email + "发送邮件", LogHelper.LogMessageType.Debug);
            string fromEmail = CFG.应用邮箱;
            string fromEmailPassword = CFG.邮箱密码.DP();
            int emailPort = String.IsNullOrEmpty(CFG.邮箱端口) ? 587 : CFG.邮箱端口.ToAs<int>();

            //"ServiceDomain".GHU() 在定时器里不能调用

            string body = new WebClient().GetHtml(CFG.本机访问地址 + "/Home/Q/" + qaId); //现在变成只能限制在本地服务器里才可以发送邮件了
            //创建邮件对象并发送
            var mail = new SendMail(email, fromEmail, body, mailTitle, fromEmailPassword, "ServiceMailName".GX(), nickName);
            //var mailRecord = new wmfMailRecord().wmfMailRecord2(email, body, "问题解答通知", "ServiceMailName".GX(), nickName, Guid.Parse(Reference.电子邮件类别_问题解答通知));
            //mrbll.Insert(mailRecord,false);
            mail.Send("smtp.", emailPort, email + "问题解答通知邮件发送失败！");
        }

        /// <summary>
        /// 用户充值数据同步
        /// </summary>
        /// <param name="SyncDT"></param>
        public void AncyRC()
        {
            var result = "";
            var dts = DateTime.Now.ToString();          
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
            string strUrl = CFG.网站域名 + CFG.数据同步_马币充值信息;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);            

            //LogHelper.Write("同步充值信息" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");

            //同步充值信息            
            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("有获取到充值数据", LogHelper.LogMessageType.Info);
                var s = "";
                try { s = DecodeJson(result); }
                catch
                {
                    s = "";
                    LogHelper.Write("解密异常", LogHelper.LogMessageType.Info);
                }
                if (!String.IsNullOrEmpty(s))
                {
                    //用户有三张表，要先分开
                    var bmRC = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);                    

                    var ubll = new BaseBll<aspnet_Users>();
                    var uids = new List<Guid>();
                    var bmRCJson = "";
                    
                    //用户需要先同步
                    if (!String.IsNullOrEmpty(bmRC))
                    {//跟UserId没关系，就不同步UserId
                        bmRCJson = Compression.DecompressString(bmRC);
                        var _list = JsonConvert.DeserializeObject<List<bmRechargeJson>>(bmRCJson);
                        if (_list.Count() > 0)
                        {
                            var uid = _list.Where(p => p.UserId != null).Select(p => p.UserId.Value);
                            if (uid.Count() > 0)
                            {
                                foreach (var u in uid)
                                {
                                    uids.Add(u);
                                }
                            }
                        }
                    }

                    //userids 去重复
                    uids = uids.Distinct().ToList();

                    //同步用户ID 下面的已经处理了哪里用户ID需要同步
                    if (uids.Count() > 0)
                        UIDAncyUser(ubll, uids);

                    //问题记录
                    try
                    {
                        //卡密充值记录
                        if (!String.IsNullOrEmpty(bmRC))
                        {
                            var _list = JsonConvert.DeserializeObject<List<bmRecharge>>(bmRCJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmRecharge>();
                                //过滤掉已经添加的数据
                                var alreadyQIds = bll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();

                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
                            }
                        }                        
                    }
                    catch
                    {
                        LogHelper.Write("用户数据获取异常导致同步充值时一些数据同步不成功", LogHelper.LogMessageType.Info);
                    }
                }
                else
                {
                    LogHelper.Write("各种原因没有获取到充值数据", LogHelper.LogMessageType.Info);
                }
            }
        }

        /// <summary>
        /// 其他数据同步前，先同步用户数据
        /// </summary>
        /// <param name="bll"></param>
        /// <param name="uids"></param>
        private void UIDAncyUser(BaseBll<aspnet_Users> bll, List<Guid> uids)
        {
            //过滤掉已经添加的数据
            var alreadyUIds = bll.All.Where(p => uids.Contains(p.UserId)).Select(p => p.UserId);
            var nuids = uids.Except(alreadyUIds).ToList().Join(CFG.邦马网_字符串分隔符);
            if(nuids.Length > 5)//需要同步时才同步
                AncyUser(null, nuids);
        }

        /// <summary>
        /// 马币充值
        /// </summary>
        public void RCMB()
        {
            var result = "";
            string strUrl = CFG.网站域名 + CFG.数据同步_服务器是否可用;
            //LogHelper.Write("同步充值信息" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl, "");
            if(result == "true")
            {                 
                //充值BLL
                var rcBll = new BaseBll<bmRecharge>();
                //取出未设置充值的卡密数据
                var wcz = Guid.Parse(Reference.卡密充值_未充值);
                var _rcList = rcBll.All.Where(p => p.Effective == null && (p.Recharge == null || p.Recharge == wcz) && p.UserId != null); //系统并发时有可能取到已充值卡密记录
                var _newRcList = new List<bmRecharge>();

                //用户马币BLL
                var bmMBRBll = new BaseBll<bmUserMaBiRecord>();
                var _sdMBR = new List<bmUserMaBiRecord>();
                //用户BLL
                var uiBll = new BaseBll<wmfUserInfo>();
                var _rzUserId = new List<Guid>();
                //卡密BLL
                var kmBll = new BaseBll<bmKaMe>();
                if (_rcList.Count() > 0)
                {
                    var rcMb = _rcList.Select(p => p.KaMe);
                
                    
                    //从已充值的卡密信息取出有效卡密
                    //取卡密时不过滤是否已充值，如果已充值，则不变更充值记录与马币记录//  这貌似不太好处理，先取未充值的卡密来操作吧
                    var _kmList = kmBll.All.Where(p => rcMb.Contains(p.KaMe)&& (p.Recharge == null || p.Recharge == wcz));

                    var ycz = Guid.Parse(Reference.卡密充值_已充值);
                    var yx = Guid.Parse(Reference.卡密有效性_有效);
                    var wx = Guid.Parse(Reference.卡密有效性_无效);

                    if(_kmList.Count() >0 )
                    {
                        var yxKaMe = _kmList.Select(p => p.KaMe);                    
                        foreach(var rc in _rcList)
                        {
                            if(yxKaMe.Contains(rc.KaMe))
                            {//有效卡密   
                                //根据卡密类型生成马币记录或认证记录
                                var ckm = _kmList.FirstOrDefault(p => p.KaMe == rc.KaMe);
                                ////卡密必须是未充值的状态才进行以下操作
                                //if(ckm.Recharge != ycz)
                                //{
                                var mbRef = Guid.Parse(Reference.马币类别_马币);
                                rc.Recharge = ycz;
                                rc.Effective = yx;
                                rc.KaMeRef = mbRef;

                                var mbNum = 10;
                                try
                                {
                                    mbNum = Convert.ToInt32(ckm.wmfReference.ItemInfo);
                                }
                                catch
                                {
                                    LogHelper.Write("卡密充值类别设置错误导致充值失误", LogHelper.LogMessageType.Info);
                                }
                                if (mbNum > 1000)
                                    mbNum = 10;

                                rc.MaBiNum = mbNum * 1000;
                                _newRcList.Add(rc);
                                if (ckm.KaMeRef == Guid.Parse(Reference.卡密类别_认证66))
                                {
                                    _rzUserId.Add(rc.UserId.Value);
                                }
                                else
                                {//根据马币类别，生成用户马币记录并添加到发送表去。保证本地的马币类别不能被修改
                                    var umbrModel = new bmUserMaBiRecord();
                                    umbrModel.SourceRef = Guid.Parse(Reference.马币来源_充值);
                                    umbrModel.MaBiRef = mbRef;                                   
                                    umbrModel.MaBiNum = mbNum * 1000;//10元=10000马币
                                    umbrModel.RCId = rc.ID;

                                    umbrModel.IsSettle = false; //充值马币时，都是未结算的，但用户可以使用。后面结算是要将未结算的标识为结算
                                    umbrModel.RegTime = DateTime.Now;
                                    umbrModel.ModTime = DateTime.Now;
                                    umbrModel.FlagTrashed = false;
                                    umbrModel.FlagDeleted = false;

                                    umbrModel.ID = Guid.NewGuid();
                                    umbrModel.UserId = rc.UserId;
                                    umbrModel.RegUser = rc.UserId;

                                    _sdMBR.Add(umbrModel);
                                    //bmMBRBll.Insert(umbrModel, false);//这边直接设置，数据同步成功之后，再统一更新到数据库
                                }
                                //}//ckm==wcz
                                //else
                                //{
                                //    rc.Recharge = wcz;
                                //    rc.Effective = wx;
                                //}
                            }
                            else
                            {//无效卡密
                                rc.Recharge = wcz;
                                rc.Effective = wx;
                                _newRcList.Add(rc);
                            }
                        }
                        //卡密设置为充值状态
                        foreach(var km in _kmList)
                        {
                            if(km.Recharge != ycz)
                            { 
                                km.Recharge = ycz;
                                km.OperateTime = DateTime.Now;
                            }
                        }
                        //设置本地需要认证的用户为认证
                        if(_rzUserId.Count() > 0)
                        {
                            var rzu = uiBll.All.Where(p => _rzUserId.Contains(p.ID));
                            foreach(var u in rzu)
                            {
                                u.CertificationLevel = Guid.Parse(Reference.认证类别_认证邦主);
                            }
                        }
                    }
                    else
                    {//如果都是无效的充值记录，直接将充值记录发送到服务器去修改记录
                        //处理充值记录
                        foreach(var rc in _rcList)
                        {
                            rc.Recharge = wcz;
                            rc.Effective = wx;
                        }
                    }
                    //与服务器数据同步成功之后，更新本地记录(处理好的充值数据，生成好的用户马币记录，需要认证的用户记录，设置好的已充值马币记录)
                    //方案修改为：为了保证成功率，本地先保存数据，然后再将数据同步到服务器，并生成数据同步记录
                    var localSave = false;
                    try
                    {
                        //先对数据进行过滤，避免并发出现多充值的情况 主要是生成的马币不能重复
                        var rcids = _sdMBR.Select(p => p.RCId);
                        var alreadyMBR = bmMBRBll.All.Where(p => rcids.Contains(p.RCId)).Select(p => p.RCId);
                        //过滤掉已经充值过的马币记录
                        _sdMBR = _sdMBR.Where(p => !alreadyMBR.Contains(p.RCId)).ToList();

                        //充值记录
                        if (_rcList.Count() > 0)
                        {
                            rcBll.UpdateChanges();
                        }
                        //生成的马币记录
                        if(_sdMBR.Count() > 0)
                        { 
                            foreach(var m in _sdMBR)
                            {
                                bmMBRBll.Insert(m, false);
                            }
                            bmMBRBll.UpdateChanges();
                        }
                        //用户认证记录
                        if (_rzUserId.Count() > 0)
                        {
                            uiBll.UpdateChanges();
                        }
                        //卡密记录
                        if (_kmList.Count() > 0)
                        {
                            kmBll.UpdateChanges();
                        }
                        localSave = true;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write("马币充值时出现异常" + ex.Message, LogHelper.LogMessageType.Info);
                    }
                    if(localSave)
                    { 
                        //同步数据到服务器有(处理好的充值数据，生成好的用户马币记录，需要认证的用户记录)
                        //转化为JSON类到JSON数据发送
                        var s = "";
                        //生成JSON数据
                        var newRCList = new List<bmRechargeJson>();
                        var newMBList = new List<bmUserMaBiRecordJson>();
                        if (_newRcList.Count() == 0)
                        {//更新后rcList会等于空，要用另外一个对象存储
                            s += " ";
                        }
                        else
                        {
                            foreach (var u in _newRcList)
                            {
                                var t = new bmRechargeJson
                                {
                                    ID = u.ID,
                                    Recharge = u.Recharge,
                                    Effective = u.Effective,
                                    KaMeRef = u.KaMeRef,
                                    MaBiNum = u.MaBiNum
                                };
                                newRCList.Add(t);
                            }
                            s += ToJsonAndCompress(newRCList);
                        }
                        s += CFG.邦马网_JSON数据间隔;

                        if (_sdMBR.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            foreach (var u in _sdMBR)
                            {
                                var t = new bmUserMaBiRecordJson
                                {
                                    ID = u.ID,
                                    UserId = u.UserId,
                                    RCId = u.RCId, //小BUG调整
                                    SourceRef = u.SourceRef,
                                    MaBiRef = u.MaBiRef,
                                    MaBiNum = u.MaBiNum,
                                    IsSettle = u.IsSettle,
                                    Sort = u.Sort,
                                    RegUser = u.RegUser,
                                    RegTime = u.RegTime,
                                    ModTime = u.ModTime,
                                    FlagTrashed = u.FlagTrashed,
                                    FlagDeleted = u.FlagDeleted
                                };
                                newMBList.Add(t);
                            }
                            s += ToJsonAndCompress(newMBList);
                        }
                        s += CFG.邦马网_JSON数据间隔;

                        if (_rzUserId.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            s += ToJsonAndCompress(_rzUserId);
                        }
                        s += CFG.邦马网_JSON数据间隔;

                        //不管发送是否成功，将发送数据保存进数据库
                        var rcsBll = new BaseBll<bmKaMeRCSend>();
                        var model = new bmKaMeRCSend();
                        model.ID = Guid.NewGuid();
                        model.SendData = s; 
                        //先设置为失败，与网站同步成功之后再设置为成功
                        var rcsb = Guid.Parse(Reference.充值数据发送_失败);
                        var rccg = Guid.Parse(Reference.充值数据发送_成功);
                        model.SendRef = rcsb;                        
                        model.RegTime = DateTime.Now;
                        model.ModTime = DateTime.Now;
                        model.FlagTrashed = false;
                        model.FlagDeleted = false;
                        rcsBll.Insert(model);

                        //向邦马网同步马币充值数据
                        result = "";
                        var dts = DateTime.Now.ToString();
                        var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                        strUrl = CFG.网站域名 + CFG.数据同步_卡密充值;

                        //未Encode的URL
                        string neAppentUrl = "?tok=" + tok;
                        if (!string.IsNullOrEmpty(s))
                        {
                            neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                        }

                        LogHelper.Write("同步马币充值信息" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                        //有传递UID时用POST方法，参数有可能会超过URL长度
                        result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                        
                        //修改本次发送的同步数据的状态为发送成功
                        if(result == "true")
                        {//与网站同步成功后，修改同步数据为成功状态
                            var srModel = rcsBll.GetModel(model.ID);
                            srModel.SendRef = rccg;
                            rcsBll.Update(srModel);
                        }
                        else
                        {
                            LogHelper.Write("马币充值信息未同步成功：" + s, LogHelper.LogMessageType.Info);
                        }
                        
                        //取出最早一条同步不成功的充值记录再同步一次
                        var nonSuccessRC = rcsBll.All.Where(p => p.SendRef == rcsb).OrderBy(p => p.RegTime).FirstOrDefault();
                        if(nonSuccessRC != null)
                        {
                            result = "";
                            dts = DateTime.Now.ToString();
                            tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                            strUrl = CFG.网站域名 + CFG.数据同步_卡密充值;

                            //未Encode的URL
                            neAppentUrl = "?tok=" + tok;
                            if (!string.IsNullOrEmpty(s))
                            {
                                neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(nonSuccessRC.SendData);
                            }

                            LogHelper.Write("同步未成功的马币充值信息" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                            result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                            //修改本次发送的同步数据的状态为发送成功
                            if (result == "true")
                            {//与网站同步成功后，修改同步数据为成功状态                                
                                nonSuccessRC.SendRef = rccg;
                                rcsBll.Update(nonSuccessRC);
                            }
                            else
                            {
                                LogHelper.Write("马币充值信息继续未同步成功：" + nonSuccessRC.SendData, LogHelper.LogMessageType.Error);
                            }
                        }
                    }
                }
            }//if 服务器可用
        }

        /// <summary>
        /// ，默认结算5天前的答题记录，结算前要确认5天前的答题是否还有未处理的异议
        /// </summary>
        public void FinalAccount()
        {
            var result = "";
            string strUrl = CFG.网站域名 + CFG.数据同步_服务器是否可用;
            //LogHelper.Write("同步充值信息" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl, "");
            if (result == "true")
            {
                //取出5天前所有未结算的问题分配并解答的记录
                var qaDisEndTime = 0 - Convert.ToDecimal(CFG.用户答题结算时间);
                var disBll = new BaseBll<bmQADistribution>();
                var fiveAgo = DateTime.Now.AddDays(Convert.ToDouble(qaDisEndTime));
                var fiveAgoQADisWJS = disBll.All.Where(p => p.OperateTime <= fiveAgo && (p.IsSettle == null || p.IsSettle == false));
                //第一步需要先同步到服务器的有 umbrListJson，qaDisJson, obJson; 本地要保存的列表fiveAgoQADisWJS(修改)，nonSettleOB(修改)，umbrList(添加)
                if (fiveAgoQADisWJS.Count() > 0)
                {
                    //检测这些记录是还存在未解决的异议
                    var qaIds = fiveAgoQADisWJS.Select(p => p.QAId);
                    //下面的删除邦马币记录需要用到
                    var disids = fiveAgoQADisWJS.Select(p => p.ID);
                    var umbrBll = new BaseBll<bmUserMaBiRecord>();
                    //添加邦马币记录前，要先删除掉本次问题记录与异议记录生成的邦马币                               
                    var bmQAdisMB = umbrBll.All.Where(p => p.QAId != null && p.DisId != null && p.OBId == null && qaIds.Contains(p.QAId.Value) && disids.Contains(p.DisId.Value));//这句很重要，要测试
                    //如果存在，先删除，可以不管后面的操作。
                    if (bmQAdisMB.Count() > 0)
                    {
                        foreach (var m in bmQAdisMB)
                        {
                            umbrBll.Delete(m, false);
                        }
                        umbrBll.UpdateChanges();
                    }

                    var obBll = new BaseBll<bmObjection>();
                    var nonOperateOB = obBll.All.Where(p => qaIds.Contains(p.QAId) && p.Result == null);
                    if (nonOperateOB.Count() > 0)
                    {
                        LogHelper.Write("自动结算时发现还有未处理的异议记录", LogHelper.LogMessageType.Info);
                    }
                    else
                    {
                        //取出所有未结算的异议记录 同步成功之后，修改为已结算并保存到数据库 //异议处理生成的邦马币与同步不在这里处理
                        var nonSettleOB = obBll.All.Where(p => qaIds.Contains(p.QAId) && p.Result != null && (p.IsSettle == null || p.IsSettle == false));

                        //答题分配记录，生成邦马币值
                        var mbRef = Guid.Parse(Reference.马币类别_马币);
                        var bbRef = Guid.Parse(Reference.马币类别_邦币);
                        var banbRef = Guid.Parse(Reference.马币类别_绑币);
                        //邦马币来源
                        var mbly_zq = Guid.Parse(Reference.马币来源_赚取);

                        var qaViewBll = new BaseBll<bmQAView>();
                        var qaViewWJS = qaViewBll.All.Where(p => qaIds.Contains(p.ID));
                        var disWeiXins = qaViewWJS.Select(p => p.DisWeiXinId).Distinct();
                        //取出所有题目的答题用户ID
                        var uwBll = new BaseBll<bmUserWeixin>();
                        var userWeixins = uwBll.All.Where(p => disWeiXins.Contains(p.WeiXinId));

                        var zqBMB = Convert.ToDecimal(CFG.答题赚取的马币比);
                        //需要同步到服务器的邦马币与本地要更新的邦马币
                        var umbrListJson = new List<bmUserMaBiRecordJson>();
                        var umbrList = new List<bmUserMaBiRecord>();
                        //根据每一道题目生成答题用户的马币与绑币记录
                        foreach (var q in qaViewWJS)
                        {
                            //问题消费的邦马币值                            
                            var qaBB = Math.Abs(q.BBNum);
                            var qaMB = Math.Abs(q.MBNum);

                            //邦马币占比
                            decimal bbPer = 0;// qaBB / (qaMB + qaBB);
                            decimal mbPer = 0;// qaMB / (qaMB + qaBB);
                            if(qaMB + qaBB != 0)
                            {
                                bbPer = qaBB / (qaMB + qaBB);
                                mbPer = qaMB / (qaMB + qaBB);
                            }

                            //回馈到答题用户去的绑币与马币值
                            var disBanB = qaBB * zqBMB * bbPer;
                            var disMB = qaMB * zqBMB * mbPer;
                            //哪个用户赚取的
                            var disUser = userWeixins.FirstOrDefault(p => p.WeiXinId == q.DisWeiXinId);
                            var disUserId = Guid.Parse(CFG.异议处理用户);
                            if (disUser != null && disUser.UserId != null)
                            {
                                disUserId = disUser.UserId.Value;
                            }
                            //生成绑币与马币记录
                            AddBMB(banbRef, mbly_zq, umbrListJson, umbrList, q, disBanB, disUserId);
                            AddBMB(mbRef, mbly_zq, umbrListJson, umbrList, q, disMB, disUserId);
                        }
                        //将分配记录标识为已结算 需要同步到服务器标识为结算的问题分配记录
                        var qaDisJson = new List<bmQADistributionJson>();
                        foreach (var d in fiveAgoQADisWJS)
                        {
                            d.IsSettle = true;
                            qaDisJson.Add(new bmQADistributionJson
                            {
                                ID = d.ID,
                                QAId = d.QAId,
                                UserId = d.UserId,
                                WeiXinId = d.WeiXinId,
                                DistributionTime = d.DistributionTime,
                                OperateTime = d.OperateTime,
                                Result = d.Result,
                                IsSettle = d.IsSettle,
                                Sort = d.Sort,
                                RegUser = d.RegUser,
                                RegTime = d.RegTime,
                                ModTime = d.ModTime,
                                FlagTrashed = d.FlagTrashed,
                                FlagDeleted = d.FlagDeleted
                            });
                        }
                        //将异议记录标识为已结算     
                        var obJson = new List<bmObjectionJson>();
                        foreach(var o in nonSettleOB)
                        {
                            o.IsSettle = true;
                            obJson.Add(new bmObjectionJson {
                                ID = o.ID,
                                QAId = o.QAId,
                                UserId = o.UserId,
                                WeiXinId = o.WeiXinId,
                                SubmitTime = o.SubmitTime,
                                ErrorNum = o.ErrorNum,
                                ObjectionExplain = o.ObjectionExplain,
                                HandleUser = o.HandleUser,
                                Result = o.Result,
                                HandleTime = o.HandleTime,
                                AllQANum = o.AllQANum,
                                ConfirmErrorNum = o.ConfirmErrorNum,
                                HandleExplain = o.HandleExplain,
                                IsSettle = o.IsSettle,
                                Sort = o.Sort,
                                RegUser = o.RegUser,
                                RegTime = o.RegTime,
                                ModTime = o.ModTime,
                                FlagTrashed = o.FlagTrashed,
                                FlagDeleted = o.FlagDeleted
                            });
                        }
                        
                        //第一次同步到服务器， 同步数据要处理：结算的分配记录，结算的异议处理，生成的用户邦马币记录(未结算)
                        var s = "";
                        if (qaDisJson.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            s += ToJsonAndCompress(qaDisJson);
                        }
                        s += CFG.邦马网_JSON数据间隔;

                        if (obJson.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            s += ToJsonAndCompress(obJson);
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

                        //向邦马网同步马币、答题与异议分配等数据
                        result = "";
                        var dts = DateTime.Now.ToString();
                        var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                        strUrl = CFG.网站域名 + CFG.数据同步_答题结算;

                        //未Encode的URL
                        string neAppentUrl = "?tok=" + tok;
                        if (!string.IsNullOrEmpty(s))
                        {
                            neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                        }

                        LogHelper.Write("同步异议答题结算信息" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                        //有传递UID时用POST方法，参数有可能会超过URL长度
                        result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                        //第一次传递邦马币记录结算完成
                        if (result == "true")
                        {
                            disBll.UpdateChanges();
                            obBll.UpdateChanges();

                            foreach (var m in umbrList)
                            {
                                umbrBll.Insert(m, false);
                            }
                            umbrBll.UpdateChanges();

                            //一更新，上面的所有变量变成空  
                            var bmUMBBll = new BaseBll<bmUserMaBi>();
                            var bmUMBSBll = new BaseBll<bmUserMaBiSettleRecord>();

                            //第二次同步开始，同步用户币记录，用户币结算记录，用户币记录结算有 bmUserMBList，
                            //取出所有未结算的邦马币记录
                            var nonSettleBMBList = umbrBll.All.Where(p => p.IsSettle == null || p.IsSettle == false);
                            if (nonSettleBMBList.Count() > 0)
                            {
                                //取出当前所有用户的邦马币值
                                //去邦马网获取需要结算的用户ID
                                var userIdsList = new List<Guid>();
                                result = "";
                                dts = DateTime.Now.ToString();
                                strUrl = CFG.网站域名 + CFG.数据同步_邦马网所有用户ID集;
                                var appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);

                                LogHelper.Write("同步问题信息" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
                                result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");
                                if (!String.IsNullOrEmpty(result))
                                {
                                    LogHelper.Write("有获取到用户数据", LogHelper.LogMessageType.Info);
                                    s = "";
                                    try { s = DecodeJson(result); }
                                    catch
                                    {
                                        s = "";
                                        LogHelper.Write("解密异常", LogHelper.LogMessageType.Info);
                                    }
                                    if (!String.IsNullOrEmpty(s))
                                    {
                                        //用户ID集
                                        var userIds = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                                        s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);

                                        if (!String.IsNullOrEmpty(userIds))
                                        {//跟UserId没关系，就不同步UserId
                                            userIds = Compression.DecompressString(userIds);
                                            userIdsList = JsonConvert.DeserializeObject<List<Guid>>(userIds);
                                        }
                                    }
                                }
                                //去邦马网获取需要结算的用户ID结束
                                if (userIdsList.Count() > 0)
                                {
                                    var bmNewUserMBList = new BaseBll<bmNewUserMB>().All.Where(p => userIdsList.Contains(p.UserId));
                                    //所有的用户邦马币值生成用户马币记录
                                    var bmUserMBList = new List<bmUserMaBi>();
                                    var bmUserMaBiSettleRecordList = new List<bmUserMaBiSettleRecord>();

                                    //结算时间 
                                    var settleTime = DateTime.Now;
                                    foreach (var numb in bmNewUserMBList)
                                    {
                                        //添加邦马币记录
                                        AddUserMB(mbRef, numb.NMB, bmUserMBList, settleTime, numb);
                                        AddUserMB(bbRef, numb.NBB, bmUserMBList, settleTime, numb);
                                        AddUserMB(banbRef, numb.NBANB, bmUserMBList, settleTime, numb);

                                        //添加邦马币结算记录
                                        AddUserMBSettle(mbRef, numb.NMB, bmUserMaBiSettleRecordList, settleTime, numb);
                                        AddUserMBSettle(bbRef, numb.NBB, bmUserMaBiSettleRecordList, settleTime, numb);
                                        AddUserMBSettle(banbRef, numb.NBANB, bmUserMaBiSettleRecordList, settleTime, numb);
                                    }

                                    //将未结算的邦马币标识为结算
                                    foreach (var um in nonSettleBMBList)
                                    {
                                        um.IsSettle = true;
                                    }

                                    //生成传递的JSON列表
                                    var bmUserMBListJson = new List<bmUserMaBiJson>();
                                    var bmUserMaBiSettleRecordListJson = new List<bmUserMaBiSettleRecordJson>();
                                    //用户的邦马币记录Json
                                    var nonSettleBMBListJson = new List<bmUserMaBiRecordJson>();
                                    //用户的邦马币记录
                                    foreach (var umb in bmUserMBList)
                                    {
                                        bmUserMBListJson.Add(new bmUserMaBiJson
                                        {
                                            ID = umb.ID,
                                            UserId = umb.UserId,
                                            MaBiRef = umb.MaBiRef,
                                            MaBiNum = umb.MaBiNum,
                                            SettleTime = umb.SettleTime,
                                            Sort = umb.Sort,
                                            RegUser = umb.RegUser,
                                            RegTime = umb.RegTime,
                                            ModTime = umb.ModTime,
                                            FlagTrashed = umb.FlagTrashed,
                                            FlagDeleted = umb.FlagDeleted
                                        });
                                    }
                                    //用户的邦马币结算记录
                                    foreach (var umbs in bmUserMaBiSettleRecordList)
                                    {
                                        bmUserMaBiSettleRecordListJson.Add(new bmUserMaBiSettleRecordJson
                                        {
                                            ID = umbs.ID,
                                            UserId = umbs.UserId,
                                            MaBiRef = umbs.MaBiRef,
                                            MaBiNum = umbs.MaBiNum,
                                            SettleTime = umbs.SettleTime,
                                            Sort = umbs.Sort,
                                            RegUser = umbs.RegUser,
                                            RegTime = umbs.RegTime,
                                            ModTime = umbs.ModTime,
                                            FlagTrashed = umbs.FlagTrashed,
                                            FlagDeleted = umbs.FlagDeleted
                                        });
                                    }
                                    //邦马币结算记录JSON
                                    foreach (var um in nonSettleBMBList)
                                    {
                                        nonSettleBMBListJson.Add(new bmUserMaBiRecordJson
                                        {
                                            ID = um.ID,
                                            UserId = um.UserId,
                                            QAId = um.QAId,
                                            DisId = um.DisId,
                                            OBId = um.OBId,
                                            RCId = um.RCId,
                                            TkId = um.TkId,
                                            SourceRef = um.SourceRef,
                                            MaBiRef = um.MaBiRef,
                                            MaBiNum = um.MaBiNum,
                                            IsSettle = um.IsSettle,
                                            Sort = um.Sort,
                                            RegUser = um.RegUser,
                                            RegTime = um.RegTime,
                                            ModTime = um.ModTime,
                                            FlagTrashed = um.FlagTrashed,
                                            FlagDeleted = um.FlagDeleted
                                        });
                                    }

                                    //邦马币结算与服务器同步
                                    s = "";
                                    if (bmUserMBListJson.Count() == 0)
                                    {
                                        s += " ";
                                    }
                                    else
                                    {
                                        s += ToJsonAndCompress(bmUserMBListJson);
                                    }
                                    s += CFG.邦马网_JSON数据间隔;

                                    if (bmUserMaBiSettleRecordListJson.Count() == 0)
                                    {
                                        s += " ";
                                    }
                                    else
                                    {
                                        s += ToJsonAndCompress(bmUserMaBiSettleRecordListJson);
                                    }
                                    s += CFG.邦马网_JSON数据间隔;

                                    if (nonSettleBMBListJson.Count() == 0)
                                    {
                                        s += " ";
                                    }
                                    else
                                    {
                                        s += ToJsonAndCompress(nonSettleBMBListJson);
                                    }
                                    s += CFG.邦马网_JSON数据间隔;

                                    //向邦马网同步马币、答题与异议分配等数据
                                    result = "";
                                    dts = DateTime.Now.ToString();
                                    strUrl = CFG.网站域名 + CFG.数据同步_邦马币结算;

                                    //未Encode的URL
                                    neAppentUrl = "?tok=" + tok;
                                    if (!string.IsNullOrEmpty(s))
                                    {
                                        neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                                    }

                                    LogHelper.Write("同步用户邦马币信息" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                                    //有传递UID时用POST方法，参数有可能会超过URL长度
                                    result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                                    if (result == "true")
                                    {
                                        //var bmUserMBList = new List<bmUserMaBi>();
                                        //var bmUserMaBiSettleRecordList = new List<bmUserMaBiSettleRecord>();
                                        //用户邦马币

                                        if (bmUserMBList.Count() > 0)
                                        {
                                            //删除现有的用户邦马币
                                            var curUMB = bmUMBBll.All;
                                            if (curUMB.Count() > 0)
                                            {
                                                foreach (var u in curUMB)
                                                {
                                                    bmUMBBll.Delete(u, false);
                                                }
                                                bmUMBBll.UpdateChanges();
                                            }

                                            foreach (var u in bmUserMBList)
                                            {
                                                bmUMBBll.Insert(u, false);
                                            }

                                        }
                                        else
                                        {
                                            LogHelper.Write("未生成用户邦马币结算记录", LogHelper.LogMessageType.Info);
                                        }


                                        //用户邦马币结算记录
                                        if (bmUserMaBiSettleRecordList.Count() > 0)
                                        {
                                            //删除当天有同步过来的用户邦马币结算记录
                                            var curUMB = bmUMBBll.All;
                                            var dt = bmUserMaBiSettleRecordList.FirstOrDefault().SettleTime;
                                            if (dt == null)
                                                dt = DateTime.Now.Date;
                                            var startdt = dt.Value.Date;
                                            var enddt = startdt.AddDays(1).AddSeconds(-1);
                                            var alreadyMBSList = bmUMBSBll.All.Where(p => p.SettleTime >= startdt && p.SettleTime <= enddt);
                                            if (alreadyMBSList.Count() > 0)
                                            {
                                                foreach (var m in alreadyMBSList)
                                                {
                                                    bmUMBSBll.Delete(m, false);
                                                }
                                                bmUMBSBll.UpdateChanges();
                                            }

                                            foreach (var u in bmUserMaBiSettleRecordList)
                                            {
                                                bmUMBSBll.Insert(u, false);
                                            }

                                        }
                                        else
                                        {
                                            LogHelper.Write("未生成用户邦马币结算记录", LogHelper.LogMessageType.Info);

                                        }
                                        //统一保存进数据库
                                        bmUMBBll.UpdateChanges();
                                        bmUMBSBll.UpdateChanges();
                                        umbrBll.UpdateChanges();
                                    }//第二次马币结算同步
                                    else
                                    {
                                        LogHelper.Write("同步邦马币结算信息时未返回正确结果" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                                    }
                                }//userIdsList.Count() > 0
                            }//nonSettleBMBList.Count() > 0
                        }//第一次传递邦马币记录结算完成
                        else
                        {
                            LogHelper.Write("同步问题结算信息时未返回正确结果" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                        }
                    }//else nonOperateOB.Count() > 0
                }//if (fiveAgoQADisWJS.Count() > 0)
            }//if 服务器可用
        }

        /// <summary>
        /// 结算用户的邦马币
        /// </summary>
        /// <param name="mbRef"></param>
        /// <param name="mbNum"></param>
        /// <param name="bmUserMBList"></param>
        /// <param name="settleTime"></param>
        /// <param name="numb"></param>
        private static void AddUserMB(Guid mbRef, decimal? mbNum, List<bmUserMaBi> bmUserMBList, DateTime settleTime, bmNewUserMB numb)
        {
            if (mbNum == null)
                mbNum = 0;
            bmUserMBList.Add(new bmUserMaBi
            {
                ID = Guid.NewGuid(),
                UserId = numb.UserId,
                MaBiRef = mbRef,
                MaBiNum = mbNum,
                SettleTime = settleTime,
                Sort = null,
                RegUser = Guid.Parse(CFG.异议处理用户),
                RegTime = settleTime,
                ModTime = settleTime,
                FlagTrashed = false,
                FlagDeleted = false
            });
        }

        ///// <summary>
        ///// 用户马币Json 记录添加
        ///// </summary>
        ///// <param name="mbRef"></param>
        ///// <param name="mbNum"></param>
        ///// <param name="bmUserMBList"></param>
        ///// <param name="settleTime"></param>
        ///// <param name="numb"></param>
        //private static void AddUserMBJson(Guid mbRef, decimal? mbNum, List<bmUserMaBiJson> bmUserMBList, DateTime settleTime, bmNewUserMB numb)
        //{
        //    if (mbNum == null)
        //        mbNum = 0;
        //    bmUserMBList.Add(new bmUserMaBiJson
        //    {
        //        ID = Guid.NewGuid(),
        //        UserId = numb.UserId,
        //        MaBiRef = mbRef,
        //        MaBiNum = mbNum,
        //        SettleTime = settleTime,
        //        Sort = null,
        //        RegUser = Guid.Parse(CFG.异议处理用户),
        //        RegTime = settleTime,
        //        ModTime = settleTime,
        //        FlagTrashed = false,
        //        FlagDeleted = false
        //    });
        //}
        /// <summary>
        /// 用户邦马币结算记录
        /// </summary>
        /// <param name="mbRef"></param>
        /// <param name="mbNum"></param>
        /// <param name="bmUserMaBiSettleRecordList"></param>
        /// <param name="settleTime"></param>
        /// <param name="numb"></param>
        private static void AddUserMBSettle(Guid mbRef, decimal? mbNum, List<bmUserMaBiSettleRecord> bmUserMaBiSettleRecordList, DateTime settleTime, bmNewUserMB numb)
        {
            if (mbNum == null)
                mbNum = 0;
            bmUserMaBiSettleRecordList.Add(new bmUserMaBiSettleRecord
            {
                ID = Guid.NewGuid(),
                UserId = numb.UserId,
                MaBiRef = mbRef,
                MaBiNum = mbNum,
                SettleTime = settleTime,
                Sort = null,
                RegUser = Guid.Parse(CFG.异议处理用户),
                RegTime = settleTime,
                ModTime = settleTime,
                FlagTrashed = false,
                FlagDeleted = false
            });
        }
        ///// <summary>
        ///// 用户邦马币结算记录的Json
        ///// </summary>
        ///// <param name="mbRef"></param>
        ///// <param name="mbNum"></param>
        ///// <param name="bmUserMaBiSettleRecordList"></param>
        ///// <param name="settleTime"></param>
        ///// <param name="numb"></param>
        //private static void AddUserMBSettleJson(Guid mbRef, decimal? mbNum, List<bmUserMaBiSettleRecordJson> bmUserMaBiSettleRecordList, DateTime settleTime, bmNewUserMB numb)
        //{
        //    if (mbNum == null)
        //        mbNum = 0;
        //    bmUserMaBiSettleRecordList.Add(new bmUserMaBiSettleRecordJson
        //    {
        //        ID = Guid.NewGuid(),
        //        UserId = numb.UserId,
        //        MaBiRef = mbRef,
        //        MaBiNum = mbNum,
        //        SettleTime = settleTime,
        //        Sort = null,
        //        RegUser = Guid.Parse(CFG.异议处理用户),
        //        RegTime = settleTime,
        //        ModTime = settleTime,
        //        FlagTrashed = false,
        //        FlagDeleted = false
        //    });
        //}

        /// <summary>
        /// 结算时调用的添加邦马币方法 
        /// </summary>
        /// <param name="mbRef"></param>
        /// <param name="mbly_zq"></param>
        /// <param name="umbrListJson"></param>
        /// <param name="umbrList"></param>
        /// <param name="q"></param>
        /// <param name="disBanB"></param>
        /// <param name="disUserId"></param>
        private static void AddBMB(Guid mbRef, Guid mbly_zq, List<bmUserMaBiRecordJson> umbrListJson, List<bmUserMaBiRecord> umbrList, bmQAView q, decimal disBanB, Guid disUserId)
        {
            var umbrModel = new bmUserMaBiRecord();
            umbrModel.ID = Guid.NewGuid();
            umbrModel.SourceRef = mbly_zq;
            umbrModel.MaBiRef = mbRef;
            umbrModel.MaBiNum = disBanB;
            umbrModel.QAId = q.ID;
            umbrModel.DisId = q.DisId;
            umbrModel.UserId = disUserId;

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
                UserId = umbrModel.UserId,

                IsSettle = umbrModel.IsSettle,
                RegUser = umbrModel.RegUser,
                RegTime = umbrModel.RegTime,
                ModTime = umbrModel.ModTime,
                FlagTrashed = umbrModel.FlagTrashed,
                FlagDeleted = umbrModel.FlagDeleted
            });
        }
        #endregion        
    }
}
