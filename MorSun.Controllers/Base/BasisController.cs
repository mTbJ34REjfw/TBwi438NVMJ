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
        private static bmNewUserMB GetUserMaBiByUId(Guid userId)
        {
            //取出当前用户已结算的马币值
            return new BaseBll<bmNewUserMB>().All.FirstOrDefault(p => p.UserId == userId);            
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
        protected static bool IsRZ(string tok, bool rz)
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
                {//限制45秒内
                    rz = false;
                }
                else
                {
                    rz = true;
                    LogHelper.Write("时间：" + ts.Substring(0, ind), LogHelper.LogMessageType.Info);
                }
            }
            catch
            {
                rz = false;
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
            var dts = DateTime.Now.ToString();//dt.ToShortDateString() + " " + dt.ToShortTimeString();            
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
                var s = DecodeJson(result);
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
            var dts = DateTime.Now.ToString();//dt.ToShortDateString() + " " + dt.ToShortTimeString();            
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
                var s = DecodeJson(result);
                if (!String.IsNullOrEmpty(s))
                {
                    //用户有三张表，要先分开
                    var bmQA = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmMB = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var qaDIS = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmOB = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmUW = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();
                    s = s.Substring(s.IndexOf(CFG.邦马网_JSON数据间隔) + CFG.邦马网_JSON数据间隔.Length);
                    var bmTK = s.Substring(0, s.IndexOf(CFG.邦马网_JSON数据间隔)).Trim();

                    var ubll = new BaseBll<aspnet_Users>();
                    var uids = new List<Guid>();
                    var bmQAJson = "";
                    var bmMBJson = "";
                    var qaDISJson = "";
                    var bmOBJson = "";
                    var bmUWJson = "";
                    var bmTKJson = "";
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

                    //userids 去重复
                    uids = uids.Distinct().ToList();

                    //同步用户ID 下面的已经处理了哪里用户ID需要同步
                    if (uids.Count() > 0)
                        UIDAncyUser(ubll, uids);

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
                                var bll = new BaseBll<bmQA>();
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
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();
                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
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
                                var bll = new BaseBll<bmQADistribution>();
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
                                var bll = new BaseBll<bmUserWeixin>();
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
                    }
                    catch
                    {
                        LogHelper.Write("用户数据获取异常导致同步问题时一些数据同步不成功", LogHelper.LogMessageType.Info);
                    }
                }                
                else
                {
                    LogHelper.Write("各种原因没有获取到问题数据", LogHelper.LogMessageType.Info);
                }            
            }            
        }


        /// <summary>
        /// 用户充值数据同步
        /// </summary>
        /// <param name="SyncDT"></param>
        public void AncyRC()
        {
            var result = "";
            var dts = DateTime.Now.ToString();//dt.ToShortDateString() + " " + dt.ToShortTimeString();            
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
            string strUrl = CFG.网站域名 + CFG.数据同步_马币充值信息;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);            

            //LogHelper.Write("同步充值信息" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");

            //同步充值信息            
            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("有获取到充值数据", LogHelper.LogMessageType.Info);
                var s = DecodeJson(result);
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
            var newRCList = new List<bmRechargeJson>();
            var rcBll = new BaseBll<bmRecharge>();
            var _rcList = rcBll.All.Where(p => p.Effective == null && p.Recharge == null);

            if (_rcList.Count() != 0)
            {
                var rcMb = _rcList.Select(p => p.KaMe);
                var kmBll = new BaseBll<bmKaMe>();
                //var _kmList = 
            }
        }

        #endregion

    }
}
