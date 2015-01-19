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
using MorSun.Common.���;
using MorSun.Common.����;
using HOHO18.Common.SSO;
using HOHO18.Common.WEB;
using HOHO18.Common.DEncrypt;
using Newtonsoft.Json;

namespace System
{
    public static class ControllerHelper
    {
        /// <summary>
        /// �ж��Ƿ���Ȩ��
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        public static bool HP(this string resourceId, string operationId)
        {
            return MorSun.Controllers.BasisController.havePrivilege(resourceId, operationId);
        }

        /// <summary>
        /// ����GUID�ַ��������������
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
        #region ���ض�����
        /// <summary>
        /// ���صĴ�������װ
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="oper"></param>
        /// <param name="defAction"></param>
        /// <param name="defController"></param>
        protected void fillOperationResult(string returnUrl, OperationResult oper, string message = "�����ɹ�", string defAction = "index", string defController = "home")
        {
            oper.ResultType = OperationResultType.Success;
            oper.Message = message;
            oper.AppendData = string.IsNullOrEmpty(returnUrl) ? Url.Action(defAction, defController) : returnUrl;
        }
        /// <summary>
        /// SSO��¼ʱʹ�ã���Ҫ��������վ
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="SSOLink"></param>
        /// <param name="oper"></param>
        /// <param name="message"></param>
        /// <param name="defAction"></param>
        /// <param name="defController"></param>
        protected void fillOperationResult(string returnUrl, string SSOLink, OperationResult oper, string message = "�����ɹ�", string defAction = "index", string defController = "home")
        {
            oper.ResultType = OperationResultType.Success;
            oper.Message = message;
            oper.AppendData = string.IsNullOrEmpty(returnUrl) ? Url.Action(defAction, defController) : returnUrl;
            oper.SSOLink = SSOLink;
        }
        #endregion  

        #region ������Ϣ
        /// <summary>
        /// �û�ID
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
        /// ��ǰ�û�������Ϣ
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
        /// �����û�IDȡ�������ֵ
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        protected static bmNewUserMB GetUserMaBiByUId(Guid userId)
        {
            //ȡ����ǰ�û�ʣ������ֵ(����δ����)
            return new BaseBll<bmNewUserMB>().All.FirstOrDefault(p => p.UserId == userId);            
        }

        /// <summary>
        /// �����û�ID��������Ƿ���ȡ��
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
            //ȡ����ǰ�û�ʣ������ֵ(����δ����)
            return new BaseBll<bmNewUserMB>().All.Where(p => uids.Contains(p.UserId));
        }
        #endregion

        /// <summary>
        /// ���ⳬ���û�
        /// </summary>
        /// <returns></returns>
        private static bool IsAU()
        {
            return UserID.ToString().Eql("FBE691B1B0C15342C9EA792B733369408D118C60F094D9440E00E4BD04B3E073B48504DB1A5F810A".DP());// ("AU".GX().DP());��ֹ�����ģ�����XML��ȡ
        }
       
        #region ��֤���ʲ���
        /// <summary>
        /// �Ƿ�Ϊ��֤����
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="rz"></param>
        /// <returns></returns>
        protected static bool IsRZ(string tok, bool rz, HttpRequestBase rq)
        {
            try
            {
                //�ж��Ƿ���������������
                var ts = SecurityHelper.Decrypt(tok);
                //ȡʱ���
                var ind = ts.IndexOf(';');
                DateTime dt = DateTime.Parse(ts.Substring(0, ind));
                //�ö�ʱ��ִ��ʱ���ӳ٣�5�벻��
                if (dt.AddSeconds(12) < DateTime.Now || !ts.Contains(CFG.������_�Խ�ͳһ��))
                {//����8����
                    rz = false;
                    LogHelper.Write("����δ��֤" + rq.RawUrl, LogHelper.LogMessageType.Info);
                }
                else
                {
                    rz = true;
                    LogHelper.Write("ʱ�䣺" + ts.Substring(0, ind), LogHelper.LogMessageType.Debug);
                }
            }
            catch
            {
                rz = false;
                LogHelper.Write("���ʸ���ԭ����֤����" + rq.RawUrl, LogHelper.LogMessageType.Error);
            }
            return rz;
        }

        /// <summary>
        /// �������л�ΪJSON��ѹ��
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        protected static string ToJsonAndCompress(object v)
        {
            var jsonV = JsonConvert.SerializeObject(v);
            return Compression.CompressString(jsonV);
        }

        /// <summary>
        /// ��JSON���м���
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
        /// ��JSON���н���
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
        

        #region Ȩ��
        public static List<wmfRolePrivilegesView> getSessionPrivileges()
        {
            if (System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] == null)
                setSessionPrivileges();
            if (String.Compare("��Ȩ��", System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] as string) == 0)
                return null;
            else
                return System.Web.HttpContext.Current.Session["SessionPrivilege"] as List<wmfRolePrivilegesView>;
        }

        /// <summary>
        /// �жϵ�ǰ�û��Ƿ��ж�ĳ��Դ�ķ���Ȩ��
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
        /// �жϵ�ǰ�û��Ƿ��ж�ĳ��Դ�ķ���Ȩ��,��������жϣ���ϵΪ��ֻҪһ��Ϊ�棬������
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
        /// �жϵ�ǰ�û��Ƿ��ж�ĳ��Դ�ķ���Ȩ��,���в�������
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="operationId"></param>
        /// <param name="privilegeValue">Ȩ�޲���</param>
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
        /// �����ݿ��ж�ȡȫ���б�
        /// <param name="userid">��ǰ�û�UserID</param>
        /// </summary>
        public static IList<wmfRolePrivilegesView> getSessionPrivilegesByDatabase(Guid userid)
        {
            var currUser = new BaseBll<aspnet_Users>().GetModel(userid);
            //ȡ����ǰ�û����е�Ȩ��-��ɫ���� ��߷������У��������
            var rolesId = currUser.aspnet_Roles.SingleOrDefault().RoleId;/*currUser.aspnet_Roles.Select(r => r.RoleId).ToArray();*/
           
            var rolePrivilegeViewBll = new BaseBll<wmfRolePrivilegesView>();
            var currentUserPrivileges = rolePrivilegeViewBll.All.Where(u => u.RoleId == rolesId).ToList();
            return currentUserPrivileges;
        }

        /// <summary>
        /// ȡ����ǰ�û���Ȩ���б�������Session�С�
        /// </summary>
        public static void setSessionPrivileges()
        {
            var sessionPrivilegeList = getSessionPrivilegesByDatabase(UserID);
            if (sessionPrivilegeList.Any())
            {
                System.Web.HttpContext.Current.Session["SessionPrivilege"] = sessionPrivilegeList;
                System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] = "��Ȩ��";//��Ȩ��ID��
            }
            else
            {
                System.Web.HttpContext.Current.Session["HaveSessionPrivilege"] = "��Ȩ��";//��û��Ȩ��ID��
            }
        }
        #endregion

        #region ������־
        /// <summary>
        /// ������־
        /// </summary>
        /// <param name="ID">�޸ļ�¼ID</param>
        /// <param name="tableName">����</param>
        /// <param name="operateContent">��������</param>
        /// <param name="originalContent">�޸�ǰ</param>
        /// <param name="afterOperateContent">�޸ĺ�</param>
        public void InsertLog(Guid? ID, string tableName, string operateContent, string originalContent, string afterOperateContent)
        {
            string result = string.Empty;
            var opeateBill = new BaseBll<wmfOperationalLogbook>();
            var model = new wmfOperationalLogbook();
            model.UserId = UserID;
            //model.ApplicationId = AppId;//Ӧ�ó���ID
            model.RegTime = DateTime.Now;//���ʱ��
            model.LinkId = ID;//�޸ļ�¼ID
            model.OperateTable = tableName;
            model.OriginalContent = originalContent;
            model.AfterOperateContent = afterOperateContent;
            model.UserIP = IPAddress;//�û�IP��ַ
            model.OperateContent = operateContent;//��������
            opeateBill.Insert(model);
        }

        /// <summary>
        /// ������־
        /// </summary>
        /// <param name="ID">�޸ļ�¼ID</param>
        /// <param name="tableName">����</param>
        /// <param name="operateContent">��������</param>
        /// <param name="originalContent">�޸�ǰ</param>
        /// <param name="afterOperateContent">�޸ĺ�</param>
        /// <param name="userID">�û�Guid</param>
        public void InsertLog(Guid? ID, string tableName, string operateContent, string originalContent, string afterOperateContent, Guid userID)
        {
            string result = string.Empty;
            var opeateBill = new BaseBll<wmfOperationalLogbook>();
            var model = new wmfOperationalLogbook();
            model.UserId = userID;
            //model.ApplicationId = AppId;//Ӧ�ó���ID
            model.RegTime = DateTime.Now;//���ʱ��
            model.LinkId = ID;//�޸ļ�¼ID
            model.OperateTable = tableName;
            model.OriginalContent = originalContent;
            model.AfterOperateContent = afterOperateContent;
            model.UserIP = IPAddress;//�û�IP��ַ
            model.OperateContent = operateContent;//��������
            opeateBill.Insert(model);
        }


        /// <summary>
        /// ������־
        /// </summary>
        /// <param name="ID">�޸ļ�¼ID</param>
        /// <param name="tableName">����</param>
        /// <param name="operateContent">��������</param>
        /// <param name="originalContent">�޸�ǰ</param>
        /// <param name="afterOperateContent">�޸ĺ�</param>
        /// <param name="userID">�û�Guid</param>
        /// <param name="businessId">ҵ��id���϶����ڲ������Ĺ��ܣ���һ����������������ܶ����Ʒ����ô�ö����ž���businessId��</param>
        public void InsertLog(Guid? ID, string tableName, string operateContent, string originalContent, string afterOperateContent, Guid userID, Guid? businessId)
        {
            string result = string.Empty;
            var opeateBill = new BaseBll<wmfOperationalLogbook>();
            var model = new wmfOperationalLogbook();
            model.UserId = userID;
            //model.ApplicationId = AppId;//Ӧ�ó���ID
            model.RegTime = DateTime.Now;//���ʱ��
            model.LinkId = ID;//�޸ļ�¼ID
            model.OperateTable = tableName;
            model.OriginalContent = originalContent;
            model.AfterOperateContent = afterOperateContent;
            model.UserIP = IPAddress;//�û�IP��ַ
            model.OperateContent = operateContent;//��������
            model.BusinessId = businessId;//ҵ��id
            opeateBill.Insert(model);
        }

        /// <summary>
        /// ������ʼ�¼
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

        #region ��ȡ�û���¼IP
        /// <summary>
        /// ��ȡ�õ�¼IP
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

        #region ͨ��Guid�ַ�����ȡ����
        /// <summary>
        /// ͨ��Guid�ַ�����ȡ����
        /// </summary>
        /// <param name="str">Guid�ַ���</param>
        /// <returns>��������</returns>
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
        /// ��ȡRef����
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static wmfReference GetRefModel(Guid? id)
        {
            return new BaseBll<wmfReference>().GetModel(id);
        }
        #endregion

        #region ��֤���жϷ���
        /// <summary>
        /// ��֤����֤
        /// </summary>
        /// <param name="model"></param>
        protected void validateVerifyCode(string verifyCode, string verifycodeRandom, string xmlconfigName)
        {
            //�ж��Ƿ���֤�뿪��
            if (xmlconfigName.GX() == "true")
            {
                //�ж���֤���Ƿ���д
                if (String.IsNullOrEmpty(verifyCode))
                {
                    "Verifycode".AE("����д��֤��", ModelState);
                }
                if (VerifyCode.GetValue(verifycodeRandom) != null)
                {
                    object vCodeVal = VerifyCode.GetValue(verifycodeRandom);
                    if (String.IsNullOrEmpty(verifyCode) || vCodeVal == null || String.Compare(verifyCode, vCodeVal.ToString()) != 0)
                    {
                        "Verifycode".AE("��֤�����", ModelState);
                    }
                    else
                    {
                        //ajax�ķ�ʽ��¼��Ҫ�ȵ�¼�ɹ�֮��������֤������
                    }
                }
                else
                {
                    "Verifycode".AE("��֤�����", ModelState);
                }
                //�����֤����Ϣ
                clearVerifyCode(verifycodeRandom);
            }
        }

        /// <summary>
        /// ��ȡ��֤������
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

        #region ���ɼ��ܴ�
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

        #region ���������ʽ
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

        #region �û��Ҽ�¼
        /// <summary>
        /// ����û��Ҽ�¼  �������û������ID
        /// </summary>
        /// <param name="uIds">�û�ID������������ӡ�</param>
        /// <param name="sr">��Դ</param>
        /// <param name="mbr">����</param>
        /// <param name="mbn">��ֵ</param>
        public void AddUMBR(AddMBRModel addMBR, bool updateChange = true)
        {
            var rbll = new BaseBll<bmUserMaBiRecord>();  
            //����û��Ƿ����
            var users = new BaseBll<aspnet_Users>().All.Where(p => addMBR.UIds.Contains(p.UserId));//�ҵõ�userId �����
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
        /// ��������Ϊ�ѽ���
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
        /// ��Ҽ�ʱͳ�� �������ݿ��
        /// </summary>
        protected void SettleMaBi()
        {
            var rbll = new BaseBll<bmUserMaBiRecord>();
            var umbbll = new BaseBll<bmUserMaBi>();
            //�û���������¼
            var nonSettleMBR = rbll.All.Where(p => p.IsSettle == false && p.UserId != null);            
            var nonSMGroup = nonSettleMBR.GroupBy(p => p.UserId);
            var userIds = nonSMGroup.Select(p => p.Key);
            //�û����ֱ�
            var userMabi = umbbll.All.Where(p => userIds.Contains(p.UserId));
            //����
            var mabi = Guid.Parse(Reference.������_���);
            var bbi = Guid.Parse(Reference.������_���);
            var banbi = Guid.Parse(Reference.������_���);

            foreach(var smg in nonSMGroup)
            {
                //�����
                var mabisum = smg.Where(p => p.MaBiRef == mabi).Sum(p => p.MaBiNum);
                if(mabisum != 0)
                {
                    var thisUserMabi = userMabi.Where(p => p.UserId == smg.Key && p.MaBiRef == mabi).FirstOrDefault();
                    if(thisUserMabi == null)
                    {//ϵͳ��û����Ӵ���ҵ����                        
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
                //�Ӱ��
                var bbisum = smg.Where(p => p.MaBiRef == bbi).Sum(p => p.MaBiNum);
                if (bbisum != 0)
                {
                    var thisUserMabi = userMabi.Where(p => p.UserId == smg.Key && p.MaBiRef == bbi).FirstOrDefault();
                    if (thisUserMabi == null)
                    {//ϵͳ��û����Ӵ���ҵ����  
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


                //�Ӱ��
                var banbisum = smg.Where(p => p.MaBiRef == banbi).Sum(p => p.MaBiNum);
                if (banbisum != 0)
                {
                    var thisUserMabi = userMabi.Where(p => p.UserId == smg.Key && p.MaBiRef == banbi).FirstOrDefault();
                    if (thisUserMabi == null)
                    {//ϵͳ��û����Ӵ���ҵ����                        
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

            //���û��Ҽ�¼����Ϊ�ѽ���
            foreach(var item in nonSettleMBR)
            {
                item.IsSettle = true;
                item.ModTime = DateTime.Now;
            }

            rbll.UpdateChanges();
        }
        /// <summary>
        /// ���ݴ���Ĳ��������û���Ҷ���
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

        #region ��ȡ�û��İ���Ϣ
        /// <summary>
        /// ��ȡ�û���΢�Ű���ҵ����Ϣ
        /// </summary>
        /// <returns></returns>
        protected bmUserWeixin GetUserBound()
        {
            var wxyy = Guid.Parse(Reference.΢��Ӧ��_��ҵ��);
            return new BaseBll<bmUserWeixin>().All.FirstOrDefault(p => p.UserId == UserID && p.WeiXinAPP == wxyy);
        }

        /// <summary>
        /// ��ȡ�û�����Ϣ
        /// </summary>
        /// <returns></returns>
        //protected UserBoundCache GetUserBoundCache()
        //{
        //    var key = CFG.΢�Ű�ǰ׺ + UserID.ToString();
        //    var ubc = CacheAccess.GetFromCache(key) as UserBoundCache;
        //    //�������Ϊ�գ���������ֵ
        //    if(ubc == null)
        //    {
        //        ubc = new UserBoundCache();
        //        ubc.UserId = UserID;
        //        Random Rdm = new Random();
        //        int iRdm = 0;
        //        do
        //        {
        //            iRdm = Rdm.Next(1, 999999);

        //        } while (GetUserBoundCodeCache(iRdm) != null);//��Ϊ�ղŻ�������
        //        //�������������뻺��
        //        var codeKey = CFG.΢�Ű�ǰ׺ + iRdm.ToString();
        //        var ubcc = new UserBoundCodeCache();
        //        ubcc.UserId = UserID;
        //        ubcc.BoundCode = iRdm;
        //        CacheAccess.InsertToCacheByTime(codeKey, ubcc, 120);//�������ڹ���
        //        ubc.BoundCode = iRdm;
        //        CacheAccess.InsertToCacheByTime(key, ubc, 120);
        //    }
        //    return ubc;
        //}

        /// <summary>
        /// ���ݰ󶨴���ȡҪ�󶨵��û�
        /// </summary>
        /// <param name="boundCode"></param>
        /// <returns></returns>
        //protected UserBoundCodeCache GetUserBoundCodeCache(int boundCode)
        //{
        //    var key = CFG.΢�Ű�ǰ׺ + boundCode.ToString();
        //    return CacheAccess.GetFromCache(key) as UserBoundCodeCache;
        //}
        #endregion

        #region ����ͬ��
        /// <summary>
        /// ͬ���û�
        /// </summary>
        /// <param name="SyncDT">ͬ����ʼʱ��</param>
        /// <param name="neURLuids">��Ҫͬ�����û�ID��e26ef963-ff8d-4569-b019-7fe16103c934,1479a879-3427-40b0-a697-b7385ad9aa6d</param>
        public void AncyUser(DateTime? SyncDT, string neURLuids)
        {
            var result = "";
            var dts = DateTime.Now.ToString();          
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.������_�Խ�ͳһ��);
            string strUrl = CFG.��վ���� + CFG.����ͬ��_�û���Ϣ;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);
            if (SyncDT != null)
                appendUrl += "&SyncDT=" + SyncDT;
            if (!string.IsNullOrEmpty(neURLuids))
            {                
                appendUrl += "&UIds=" + HttpUtility.UrlEncode(neURLuids);
            }

            //δEncode��URL
            string neAppentUrl = "?tok=" + tok;
            neAppentUrl += "&SyncDT=" + SyncDT;
            if (!string.IsNullOrEmpty(neURLuids))
            {
                neAppentUrl += "&UIds=" + SecurityHelper.Encrypt(neURLuids);
            }

            LogHelper.Write("ͬ���û���Ϣ" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            if (String.IsNullOrEmpty(neURLuids))// && SyncDT == null)
            {
                //��������UIDʱ
                result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");
            }
            else
            {
                //�д���UIDʱ��POST�����������п��ܻᳬ��URL����
                result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
            }

            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("�л�ȡ���û�����", LogHelper.LogMessageType.Info);
                var s = "";
                try { s = DecodeJson(result); }
                catch
                {
                    s = "";
                    LogHelper.Write("�����쳣", LogHelper.LogMessageType.Info);
                }
                if (!String.IsNullOrEmpty(s))
                {
                    //�û������ű�Ҫ�ȷֿ�
                    var aspUS = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var aspMB = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var wmfUI = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();

                    var uids = new List<Guid>();
                    //���ݴ���
                    if (!String.IsNullOrEmpty(aspUS))
                    {                        
                        aspUS = Compression.DecompressString(aspUS);
                        var _list = JsonConvert.DeserializeObject<List<aspnet_Users>>(aspUS);
                        if (_list.Count() > 0)
                        {
                            uids = _list.Select(p => p.UserId).ToList();
                            var bll = new BaseBll<aspnet_Users>();
                            //���˵��Ѿ���ӵ�����
                            var alreadyUIds = bll.All.Where(p => uids.Contains(p.UserId)).Select(p => p.UserId);
                            uids = uids.Except(alreadyUIds).ToList();
                            _list = _list.Where(p => uids.Contains(p.UserId)).ToList();

                            foreach (var l in _list)
                            {
                                bll.Insert(l, false);
                            }
                            bll.UpdateChanges();

                            Guid rid = Guid.Parse(CFG.ע��Ĭ�Ͻ�ɫ);
                            var role = new BaseBll<aspnet_Roles>().All.Where(p => p.RoleId == rid);
                            var constr = "";
                            if (role != null)
                            {
                                foreach (var l in _list)
                                {
                                    constr += @"Insert Into aspnet_UsersInRoles ([UserId],[RoleId])  VALUES ('" + l.UserId + "','" + CFG.ע��Ĭ�Ͻ�ɫ + "')";
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
                            //���˵��Ѿ���ӵ�����                                                       
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
                            //���˵��Ѿ���ӵ�����                                
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
                LogHelper.Write("����ԭ��û�л�ȡ���û�����", LogHelper.LogMessageType.Info);
            }
        }

        /// <summary>
        /// ���⡢�û�������ͬ��
        /// </summary>
        /// <param name="SyncDT"></param>
        public void AncyQA(DateTime? SyncDT)
        {
            var result = "";
            var dts = DateTime.Now.ToString();          
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.������_�Խ�ͳһ��);
            string strUrl = CFG.��վ���� + CFG.����ͬ��_��ҵ����Ϣ;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);
            if (SyncDT != null)
                appendUrl += "&SyncDT=" + SyncDT;

            LogHelper.Write("ͬ��������Ϣ" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");


            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("�л�ȡ����������", LogHelper.LogMessageType.Info);
                var s = "";
                try { s = DecodeJson(result); }
                catch { s = "";
                LogHelper.Write("�����쳣", LogHelper.LogMessageType.Info);
                }
                if (!String.IsNullOrEmpty(s))
                {
                    //�û������ű�Ҫ�ȷֿ�
                    var bmQA = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);                    
                    var qaDIS = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmOB = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmUW = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmTK = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmMB = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);

                    var ubll = new BaseBll<aspnet_Users>();
                    var uids = new List<Guid>();
                    var bmQAJson = "";                    
                    var qaDISJson = "";
                    var bmOBJson = "";
                    var bmUWJson = "";
                    var bmTKJson = "";
                    var bmMBJson = "";
                    //�û���Ҫ��ͬ��
                    if (!String.IsNullOrEmpty(bmQA))
                    {//��UserIdû��ϵ���Ͳ�ͬ��UserId
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
                    {//��UserIdû��ϵ���Ͳ�ͬ��UserId
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

                    //userids ȥ�ظ�
                    uids = uids.Distinct().ToList();

                    //ͬ���û�ID ������Ѿ������������û�ID��Ҫͬ��
                    if (uids.Count() > 0)
                        UIDAncyUser(ubll, uids);

                    var qabll = new BaseBll<bmQA>();
                    var qadisbll = new BaseBll<bmQADistribution>();
                    var uwbll = new BaseBll<bmUserWeixin>();
                    //ͬ�������Ĵ����¼
                    var bmQAIDList = new List<Guid>();
                    //ͬ�����������鴦���¼
                    var bmQADisList = new List<Guid>();
                    //�����¼
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
                                
                                //���˵��Ѿ���ӵ�����
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
                        
                        //�������
                        if (!String.IsNullOrEmpty(qaDIS))
                        {
                            //qaDIS = Compression.DecompressString(qaDIS);
                            var _list = JsonConvert.DeserializeObject<List<bmQADistribution>>(qaDISJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                
                                //���˵��Ѿ���ӵ�����  
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
                        //�������
                        if (!String.IsNullOrEmpty(bmOB))
                        {
                            //bmOB = Compression.DecompressString(bmOB);
                            var _list = JsonConvert.DeserializeObject<List<bmObjection>>(bmOBJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmObjection>();
                                //���˵��Ѿ���ӵ�����  
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
                        //΢�Ű�
                        if (!String.IsNullOrEmpty(bmUW))
                        {
                            //bmUW = Compression.DecompressString(bmUW);
                            var _list = JsonConvert.DeserializeObject<List<bmUserWeixin>>(bmUWJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                
                                //���˵��Ѿ���ӵ�����  
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
                        
                        //ȡ��
                        if (!String.IsNullOrEmpty(bmTK))
                        {
                            //bmUW = Compression.DecompressString(bmUW);
                            var _list = JsonConvert.DeserializeObject<List<bmTakeNow>>(bmTKJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmTakeNow>();
                                //���˵��Ѿ���ӵ�����  
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

                        //��Ҽ�¼
                        if (!String.IsNullOrEmpty(bmMB))
                        {
                            //bmMB = Compression.DecompressString(bmMB);
                            var _list = JsonConvert.DeserializeObject<List<bmUserMaBiRecord>>(bmMBJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmUserMaBiRecord>();
                                //���˵��Ѿ���ӵ�����                    
                                var alreadyQIds = bll.All.Where(p => aids.Contains(p.ID)).Select(p => p.ID);
                                aids = aids.Except(alreadyQIds).ToList();
                                //����ʱ�䳬������1Сʱ������

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
                        LogHelper.Write("�û����ݻ�ȡ�쳣����ͬ������ʱһЩ����ͬ�����ɹ�", LogHelper.LogMessageType.Info);
                    }
                    //�Ѿ��ش�����ⷢ���ʼ�֪ͨ������Ա,���Է��ʼ�֪ͨʱ��Ч�ʣ�ÿ��һ���ʼ�֮ǰ���Ὣ�ʼ����ݱ�������ݿ�
                    if(bmQAIDList.Count() >0)
                    {
                        bmQAIDList = bmQAIDList.Distinct().ToList();
                        var mrbll = new BaseBll<wmfMailRecord>();                    
                        var qaList = qabll.All.Where(p => bmQAIDList.Contains(p.ID));
                        var qaWeiXinIds = qaList.Select(p => p.WeiXinId);             
                        //�����û�
                        var userWeiXins = uwbll.All.Where(p => qaWeiXinIds.Contains(p.WeiXinId));

                        var disList = qadisbll.All.Where(p => bmQADisList.Contains(p.ID));
                        var qaDisWeiXinIds = disList.Select(p => p.WeiXinId);
                        var disWeiXins = uwbll.All.Where(p => qaDisWeiXinIds.Contains(p.WeiXinId));

                        foreach (var d in qaList)
                        {                            
                            try
                            {
                                var qaU = userWeiXins.FirstOrDefault(p => p.WeiXinId == d.WeiXinId).aspnet_Users1;
                                var tt = "���ύ�������ѽ��";
                                tt += d.AutoGrenteId.ToString();                               
                                JDQAMail(mrbll, qaU.UserName, qaU.wmfUserInfo.NickName, d.ID.ToString(), tt);                                
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Write("�������ʼ����ɹ� ����¼ID��" + d.ID + " ������Ϣ" + ex.Message, LogHelper.LogMessageType.Info);
                            }
                        }
                        foreach(var d in disList)
                        {
                            try
                            {
                                var tt = "���ѽ��������";
                                tt += d.bmQA.AutoGrenteId.ToString();                                
                                var disU = disWeiXins.FirstOrDefault(p => p.WeiXinId == d.WeiXinId).aspnet_Users1;
                                JDQAMail(mrbll, disU.UserName, disU.wmfUserInfo.NickName, d.bmQA.ID.ToString(), tt);  
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Write("�������ʼ����ɹ� ����¼ID��" + d.ID + " ������Ϣ" + ex.Message, LogHelper.LogMessageType.Info);
                            }
                        }
                        //mrbll.UpdateChanges();//��߱��淢���ʼ�������Ͳ�������
                    }
                }                
                else
                {
                    LogHelper.Write("����ԭ��û�л�ȡ����������", LogHelper.LogMessageType.Info);
                }            
            }            
        }

        /// <summary>
        /// �������֪ͨ������Ա
        /// </summary>
        /// <param name="mrbll"></param>
        /// <param name="email"></param>
        /// <param name="nickName"></param>
        /// <param name="takeMB"></param>
        /// <param name="takeMoney"></param>
        public static void JDQAMail(BaseBll<wmfMailRecord> mrbll, string email, string nickName, string qaId, string mailTitle)
        {
            LogHelper.Write(email + "�����ʼ�", LogHelper.LogMessageType.Debug);
            string fromEmail = CFG.Ӧ������;
            string fromEmailPassword = CFG.��������.DP();
            int emailPort = String.IsNullOrEmpty(CFG.����˿�) ? 587 : CFG.����˿�.ToAs<int>();

            //"ServiceDomain".GHU() �ڶ�ʱ���ﲻ�ܵ���

            string body = new WebClient().GetHtml(CFG.�������ʵ�ַ + "/Home/Q/" + qaId); //���ڱ��ֻ�������ڱ��ط�������ſ��Է����ʼ���
            //�����ʼ����󲢷���
            var mail = new SendMail(email, fromEmail, body, mailTitle, fromEmailPassword, "ServiceMailName".GX(), nickName);
            //var mailRecord = new wmfMailRecord().wmfMailRecord2(email, body, "������֪ͨ", "ServiceMailName".GX(), nickName, Guid.Parse(Reference.�����ʼ����_������֪ͨ));
            //mrbll.Insert(mailRecord,false);
            mail.Send("smtp.", emailPort, email + "������֪ͨ�ʼ�����ʧ�ܣ�");
        }

        /// <summary>
        /// �û���ֵ����ͬ��
        /// </summary>
        /// <param name="SyncDT"></param>
        public void AncyRC()
        {
            var result = "";
            var dts = DateTime.Now.ToString();          
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.������_�Խ�ͳһ��);
            string strUrl = CFG.��վ���� + CFG.����ͬ��_��ҳ�ֵ��Ϣ;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);            

            //LogHelper.Write("ͬ����ֵ��Ϣ" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");

            //ͬ����ֵ��Ϣ            
            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("�л�ȡ����ֵ����", LogHelper.LogMessageType.Info);
                var s = "";
                try { s = DecodeJson(result); }
                catch
                {
                    s = "";
                    LogHelper.Write("�����쳣", LogHelper.LogMessageType.Info);
                }
                if (!String.IsNullOrEmpty(s))
                {
                    //�û������ű�Ҫ�ȷֿ�
                    var bmRC = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);                    

                    var ubll = new BaseBll<aspnet_Users>();
                    var uids = new List<Guid>();
                    var bmRCJson = "";
                    
                    //�û���Ҫ��ͬ��
                    if (!String.IsNullOrEmpty(bmRC))
                    {//��UserIdû��ϵ���Ͳ�ͬ��UserId
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

                    //userids ȥ�ظ�
                    uids = uids.Distinct().ToList();

                    //ͬ���û�ID ������Ѿ������������û�ID��Ҫͬ��
                    if (uids.Count() > 0)
                        UIDAncyUser(ubll, uids);

                    //�����¼
                    try
                    {
                        //���ܳ�ֵ��¼
                        if (!String.IsNullOrEmpty(bmRC))
                        {
                            var _list = JsonConvert.DeserializeObject<List<bmRecharge>>(bmRCJson);
                            if (_list.Count() > 0)
                            {
                                var aids = new List<Guid>();
                                aids = _list.Select(p => p.ID).ToList();
                                var bll = new BaseBll<bmRecharge>();
                                //���˵��Ѿ���ӵ�����
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
                        LogHelper.Write("�û����ݻ�ȡ�쳣����ͬ����ֵʱһЩ����ͬ�����ɹ�", LogHelper.LogMessageType.Info);
                    }
                }
                else
                {
                    LogHelper.Write("����ԭ��û�л�ȡ����ֵ����", LogHelper.LogMessageType.Info);
                }
            }
        }

        /// <summary>
        /// ��������ͬ��ǰ����ͬ���û�����
        /// </summary>
        /// <param name="bll"></param>
        /// <param name="uids"></param>
        private void UIDAncyUser(BaseBll<aspnet_Users> bll, List<Guid> uids)
        {
            //���˵��Ѿ���ӵ�����
            var alreadyUIds = bll.All.Where(p => uids.Contains(p.UserId)).Select(p => p.UserId);
            var nuids = uids.Except(alreadyUIds).ToList().Join(CFG.������_�ַ����ָ���);
            if(nuids.Length > 5)//��Ҫͬ��ʱ��ͬ��
                AncyUser(null, nuids);
        }

        /// <summary>
        /// ��ҳ�ֵ
        /// </summary>
        public void RCMB()
        {
            var result = "";
            string strUrl = CFG.��վ���� + CFG.����ͬ��_�������Ƿ����;
            //LogHelper.Write("ͬ����ֵ��Ϣ" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl, "");
            if(result == "true")
            {                 
                //��ֵBLL
                var rcBll = new BaseBll<bmRecharge>();
                //ȡ��δ���ó�ֵ�Ŀ�������
                var wcz = Guid.Parse(Reference.���ܳ�ֵ_δ��ֵ);
                var _rcList = rcBll.All.Where(p => p.Effective == null && (p.Recharge == null || p.Recharge == wcz) && p.UserId != null); //ϵͳ����ʱ�п���ȡ���ѳ�ֵ���ܼ�¼
                var _newRcList = new List<bmRecharge>();

                //�û����BLL
                var bmMBRBll = new BaseBll<bmUserMaBiRecord>();
                var _sdMBR = new List<bmUserMaBiRecord>();
                //�û�BLL
                var uiBll = new BaseBll<wmfUserInfo>();
                var _rzUserId = new List<Guid>();
                //����BLL
                var kmBll = new BaseBll<bmKaMe>();
                if (_rcList.Count() > 0)
                {
                    var rcMb = _rcList.Select(p => p.KaMe);
                
                    
                    //���ѳ�ֵ�Ŀ�����Ϣȡ����Ч����
                    //ȡ����ʱ�������Ƿ��ѳ�ֵ������ѳ�ֵ���򲻱����ֵ��¼����Ҽ�¼//  ��ò�Ʋ�̫�ô�����ȡδ��ֵ�Ŀ�����������
                    var _kmList = kmBll.All.Where(p => rcMb.Contains(p.KaMe)&& (p.Recharge == null || p.Recharge == wcz));

                    var ycz = Guid.Parse(Reference.���ܳ�ֵ_�ѳ�ֵ);
                    var yx = Guid.Parse(Reference.������Ч��_��Ч);
                    var wx = Guid.Parse(Reference.������Ч��_��Ч);

                    if(_kmList.Count() >0 )
                    {
                        var yxKaMe = _kmList.Select(p => p.KaMe);                    
                        foreach(var rc in _rcList)
                        {
                            if(yxKaMe.Contains(rc.KaMe))
                            {//��Ч����   
                                //���ݿ�������������Ҽ�¼����֤��¼
                                var ckm = _kmList.FirstOrDefault(p => p.KaMe == rc.KaMe);
                                ////���ܱ�����δ��ֵ��״̬�Ž������²���
                                //if(ckm.Recharge != ycz)
                                //{
                                var mbRef = Guid.Parse(Reference.������_���);
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
                                    LogHelper.Write("���ܳ�ֵ������ô����³�ֵʧ��", LogHelper.LogMessageType.Info);
                                }
                                if (mbNum > 1000)
                                    mbNum = 10;

                                rc.MaBiNum = mbNum * 1000;
                                _newRcList.Add(rc);
                                if (ckm.KaMeRef == Guid.Parse(Reference.�������_��֤66))
                                {
                                    _rzUserId.Add(rc.UserId.Value);
                                }
                                else
                                {//���������������û���Ҽ�¼����ӵ����ͱ�ȥ����֤���ص��������ܱ��޸�
                                    var umbrModel = new bmUserMaBiRecord();
                                    umbrModel.SourceRef = Guid.Parse(Reference.�����Դ_��ֵ);
                                    umbrModel.MaBiRef = mbRef;                                   
                                    umbrModel.MaBiNum = mbNum * 1000;//10Ԫ=10000���
                                    umbrModel.RCId = rc.ID;

                                    umbrModel.IsSettle = false; //��ֵ���ʱ������δ����ģ����û�����ʹ�á����������Ҫ��δ����ı�ʶΪ����
                                    umbrModel.RegTime = DateTime.Now;
                                    umbrModel.ModTime = DateTime.Now;
                                    umbrModel.FlagTrashed = false;
                                    umbrModel.FlagDeleted = false;

                                    umbrModel.ID = Guid.NewGuid();
                                    umbrModel.UserId = rc.UserId;
                                    umbrModel.RegUser = rc.UserId;

                                    _sdMBR.Add(umbrModel);
                                    //bmMBRBll.Insert(umbrModel, false);//���ֱ�����ã�����ͬ���ɹ�֮����ͳһ���µ����ݿ�
                                }
                                //}//ckm==wcz
                                //else
                                //{
                                //    rc.Recharge = wcz;
                                //    rc.Effective = wx;
                                //}
                            }
                            else
                            {//��Ч����
                                rc.Recharge = wcz;
                                rc.Effective = wx;
                                _newRcList.Add(rc);
                            }
                        }
                        //��������Ϊ��ֵ״̬
                        foreach(var km in _kmList)
                        {
                            if(km.Recharge != ycz)
                            { 
                                km.Recharge = ycz;
                                km.OperateTime = DateTime.Now;
                            }
                        }
                        //���ñ�����Ҫ��֤���û�Ϊ��֤
                        if(_rzUserId.Count() > 0)
                        {
                            var rzu = uiBll.All.Where(p => _rzUserId.Contains(p.ID));
                            foreach(var u in rzu)
                            {
                                u.CertificationLevel = Guid.Parse(Reference.��֤���_��֤����);
                            }
                        }
                    }
                    else
                    {//���������Ч�ĳ�ֵ��¼��ֱ�ӽ���ֵ��¼���͵�������ȥ�޸ļ�¼
                        //�����ֵ��¼
                        foreach(var rc in _rcList)
                        {
                            rc.Recharge = wcz;
                            rc.Effective = wx;
                        }
                    }
                    //�����������ͬ���ɹ�֮�󣬸��±��ؼ�¼(����õĳ�ֵ���ݣ����ɺõ��û���Ҽ�¼����Ҫ��֤���û���¼�����úõ��ѳ�ֵ��Ҽ�¼)
                    //�����޸�Ϊ��Ϊ�˱�֤�ɹ��ʣ������ȱ������ݣ�Ȼ���ٽ�����ͬ����������������������ͬ����¼
                    var localSave = false;
                    try
                    {
                        //�ȶ����ݽ��й��ˣ����Ⲣ�����ֶ��ֵ����� ��Ҫ�����ɵ���Ҳ����ظ�
                        var rcids = _sdMBR.Select(p => p.RCId);
                        var alreadyMBR = bmMBRBll.All.Where(p => rcids.Contains(p.RCId)).Select(p => p.RCId);
                        //���˵��Ѿ���ֵ������Ҽ�¼
                        _sdMBR = _sdMBR.Where(p => !alreadyMBR.Contains(p.RCId)).ToList();

                        //��ֵ��¼
                        if (_rcList.Count() > 0)
                        {
                            rcBll.UpdateChanges();
                        }
                        //���ɵ���Ҽ�¼
                        if(_sdMBR.Count() > 0)
                        { 
                            foreach(var m in _sdMBR)
                            {
                                bmMBRBll.Insert(m, false);
                            }
                            bmMBRBll.UpdateChanges();
                        }
                        //�û���֤��¼
                        if (_rzUserId.Count() > 0)
                        {
                            uiBll.UpdateChanges();
                        }
                        //���ܼ�¼
                        if (_kmList.Count() > 0)
                        {
                            kmBll.UpdateChanges();
                        }
                        localSave = true;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write("��ҳ�ֵʱ�����쳣" + ex.Message, LogHelper.LogMessageType.Info);
                    }
                    if(localSave)
                    { 
                        //ͬ�����ݵ���������(����õĳ�ֵ���ݣ����ɺõ��û���Ҽ�¼����Ҫ��֤���û���¼)
                        //ת��ΪJSON�ൽJSON���ݷ���
                        var s = "";
                        //����JSON����
                        var newRCList = new List<bmRechargeJson>();
                        var newMBList = new List<bmUserMaBiRecordJson>();
                        if (_newRcList.Count() == 0)
                        {//���º�rcList����ڿգ�Ҫ������һ������洢
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
                        s += CFG.������_JSON���ݼ��;

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
                                    RCId = u.RCId, //СBUG����
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
                        s += CFG.������_JSON���ݼ��;

                        if (_rzUserId.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            s += ToJsonAndCompress(_rzUserId);
                        }
                        s += CFG.������_JSON���ݼ��;

                        //���ܷ����Ƿ�ɹ������������ݱ�������ݿ�
                        var rcsBll = new BaseBll<bmKaMeRCSend>();
                        var model = new bmKaMeRCSend();
                        model.ID = Guid.NewGuid();
                        model.SendData = s; 
                        //������Ϊʧ�ܣ�����վͬ���ɹ�֮��������Ϊ�ɹ�
                        var rcsb = Guid.Parse(Reference.��ֵ���ݷ���_ʧ��);
                        var rccg = Guid.Parse(Reference.��ֵ���ݷ���_�ɹ�);
                        model.SendRef = rcsb;                        
                        model.RegTime = DateTime.Now;
                        model.ModTime = DateTime.Now;
                        model.FlagTrashed = false;
                        model.FlagDeleted = false;
                        rcsBll.Insert(model);

                        //�������ͬ����ҳ�ֵ����
                        result = "";
                        var dts = DateTime.Now.ToString();
                        var tok = SecurityHelper.Encrypt(dts + ";" + CFG.������_�Խ�ͳһ��);
                        strUrl = CFG.��վ���� + CFG.����ͬ��_���ܳ�ֵ;

                        //δEncode��URL
                        string neAppentUrl = "?tok=" + tok;
                        if (!string.IsNullOrEmpty(s))
                        {
                            neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                        }

                        LogHelper.Write("ͬ����ҳ�ֵ��Ϣ" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                        //�д���UIDʱ��POST�����������п��ܻᳬ��URL����
                        result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                        
                        //�޸ı��η��͵�ͬ�����ݵ�״̬Ϊ���ͳɹ�
                        if(result == "true")
                        {//����վͬ���ɹ����޸�ͬ������Ϊ�ɹ�״̬
                            var srModel = rcsBll.GetModel(model.ID);
                            srModel.SendRef = rccg;
                            rcsBll.Update(srModel);
                        }
                        else
                        {
                            LogHelper.Write("��ҳ�ֵ��Ϣδͬ���ɹ���" + s, LogHelper.LogMessageType.Info);
                        }
                        
                        //ȡ������һ��ͬ�����ɹ��ĳ�ֵ��¼��ͬ��һ��
                        var nonSuccessRC = rcsBll.All.Where(p => p.SendRef == rcsb).OrderBy(p => p.RegTime).FirstOrDefault();
                        if(nonSuccessRC != null)
                        {
                            result = "";
                            dts = DateTime.Now.ToString();
                            tok = SecurityHelper.Encrypt(dts + ";" + CFG.������_�Խ�ͳһ��);
                            strUrl = CFG.��վ���� + CFG.����ͬ��_���ܳ�ֵ;

                            //δEncode��URL
                            neAppentUrl = "?tok=" + tok;
                            if (!string.IsNullOrEmpty(s))
                            {
                                neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(nonSuccessRC.SendData);
                            }

                            LogHelper.Write("ͬ��δ�ɹ�����ҳ�ֵ��Ϣ" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                            result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                            //�޸ı��η��͵�ͬ�����ݵ�״̬Ϊ���ͳɹ�
                            if (result == "true")
                            {//����վͬ���ɹ����޸�ͬ������Ϊ�ɹ�״̬                                
                                nonSuccessRC.SendRef = rccg;
                                rcsBll.Update(nonSuccessRC);
                            }
                            else
                            {
                                LogHelper.Write("��ҳ�ֵ��Ϣ����δͬ���ɹ���" + nonSuccessRC.SendData, LogHelper.LogMessageType.Error);
                            }
                        }
                    }
                }
            }//if ����������
        }

        /// <summary>
        /// ��Ĭ�Ͻ���5��ǰ�Ĵ����¼������ǰҪȷ��5��ǰ�Ĵ����Ƿ���δ���������
        /// </summary>
        public void FinalAccount()
        {
            var result = "";
            string strUrl = CFG.��վ���� + CFG.����ͬ��_�������Ƿ����;
            //LogHelper.Write("ͬ����ֵ��Ϣ" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl, "");
            if (result == "true")
            {
                //ȡ��5��ǰ����δ�����������䲢���ļ�¼
                var qaDisEndTime = 0 - Convert.ToDecimal(CFG.�û��������ʱ��);
                var disBll = new BaseBll<bmQADistribution>();
                var fiveAgo = DateTime.Now.AddDays(Convert.ToDouble(qaDisEndTime));
                var fiveAgoQADisWJS = disBll.All.Where(p => p.OperateTime <= fiveAgo && (p.IsSettle == null || p.IsSettle == false));
                //��һ����Ҫ��ͬ�������������� umbrListJson��qaDisJson, obJson; ����Ҫ������б�fiveAgoQADisWJS(�޸�)��nonSettleOB(�޸�)��umbrList(���)
                if (fiveAgoQADisWJS.Count() > 0)
                {
                    //�����Щ��¼�ǻ�����δ���������
                    var qaIds = fiveAgoQADisWJS.Select(p => p.QAId);
                    //�����ɾ������Ҽ�¼��Ҫ�õ�
                    var disids = fiveAgoQADisWJS.Select(p => p.ID);
                    var umbrBll = new BaseBll<bmUserMaBiRecord>();
                    //��Ӱ���Ҽ�¼ǰ��Ҫ��ɾ�������������¼�������¼���ɵİ����                               
                    var bmQAdisMB = umbrBll.All.Where(p => p.QAId != null && p.DisId != null && p.OBId == null && qaIds.Contains(p.QAId.Value) && disids.Contains(p.DisId.Value));//������Ҫ��Ҫ����
                    //������ڣ���ɾ�������Բ��ܺ���Ĳ�����
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
                        LogHelper.Write("�Զ�����ʱ���ֻ���δ����������¼", LogHelper.LogMessageType.Info);
                    }
                    else
                    {
                        //ȡ������δ����������¼ ͬ���ɹ�֮���޸�Ϊ�ѽ��㲢���浽���ݿ� //���鴦�����ɵİ������ͬ���������ﴦ��
                        var nonSettleOB = obBll.All.Where(p => qaIds.Contains(p.QAId) && p.Result != null && (p.IsSettle == null || p.IsSettle == false));

                        //��������¼�����ɰ����ֵ
                        var mbRef = Guid.Parse(Reference.������_���);
                        var bbRef = Guid.Parse(Reference.������_���);
                        var banbRef = Guid.Parse(Reference.������_���);
                        //�������Դ
                        var mbly_zq = Guid.Parse(Reference.�����Դ_׬ȡ);

                        var qaViewBll = new BaseBll<bmQAView>();
                        var qaViewWJS = qaViewBll.All.Where(p => qaIds.Contains(p.ID));
                        var disWeiXins = qaViewWJS.Select(p => p.DisWeiXinId).Distinct();
                        //ȡ��������Ŀ�Ĵ����û�ID
                        var uwBll = new BaseBll<bmUserWeixin>();
                        var userWeixins = uwBll.All.Where(p => disWeiXins.Contains(p.WeiXinId));

                        var zqBMB = Convert.ToDecimal(CFG.����׬ȡ����ұ�);
                        //��Ҫͬ�����������İ�����뱾��Ҫ���µİ����
                        var umbrListJson = new List<bmUserMaBiRecordJson>();
                        var umbrList = new List<bmUserMaBiRecord>();
                        //����ÿһ����Ŀ���ɴ����û���������Ҽ�¼
                        foreach (var q in qaViewWJS)
                        {
                            //�������ѵİ����ֵ                            
                            var qaBB = Math.Abs(q.BBNum);
                            var qaMB = Math.Abs(q.MBNum);

                            //�����ռ��
                            decimal bbPer = 0;// qaBB / (qaMB + qaBB);
                            decimal mbPer = 0;// qaMB / (qaMB + qaBB);
                            if(qaMB + qaBB != 0)
                            {
                                bbPer = qaBB / (qaMB + qaBB);
                                mbPer = qaMB / (qaMB + qaBB);
                            }

                            //�����������û�ȥ�İ�������ֵ
                            var disBanB = qaBB * zqBMB * bbPer;
                            var disMB = qaMB * zqBMB * mbPer;
                            //�ĸ��û�׬ȡ��
                            var disUser = userWeixins.FirstOrDefault(p => p.WeiXinId == q.DisWeiXinId);
                            var disUserId = Guid.Parse(CFG.���鴦���û�);
                            if (disUser != null && disUser.UserId != null)
                            {
                                disUserId = disUser.UserId.Value;
                            }
                            //���ɰ������Ҽ�¼
                            AddBMB(banbRef, mbly_zq, umbrListJson, umbrList, q, disBanB, disUserId);
                            AddBMB(mbRef, mbly_zq, umbrListJson, umbrList, q, disMB, disUserId);
                        }
                        //�������¼��ʶΪ�ѽ��� ��Ҫͬ������������ʶΪ�������������¼
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
                        //�������¼��ʶΪ�ѽ���     
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
                        
                        //��һ��ͬ������������ ͬ������Ҫ��������ķ����¼����������鴦�����ɵ��û�����Ҽ�¼(δ����)
                        var s = "";
                        if (qaDisJson.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            s += ToJsonAndCompress(qaDisJson);
                        }
                        s += CFG.������_JSON���ݼ��;

                        if (obJson.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            s += ToJsonAndCompress(obJson);
                        }
                        s += CFG.������_JSON���ݼ��;

                        if (umbrListJson.Count() == 0)
                        {
                            s += " ";
                        }
                        else
                        {
                            s += ToJsonAndCompress(umbrListJson);
                        }
                        s += CFG.������_JSON���ݼ��;

                        //�������ͬ����ҡ�������������������
                        result = "";
                        var dts = DateTime.Now.ToString();
                        var tok = SecurityHelper.Encrypt(dts + ";" + CFG.������_�Խ�ͳһ��);
                        strUrl = CFG.��վ���� + CFG.����ͬ��_�������;

                        //δEncode��URL
                        string neAppentUrl = "?tok=" + tok;
                        if (!string.IsNullOrEmpty(s))
                        {
                            neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                        }

                        LogHelper.Write("ͬ��������������Ϣ" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                        //�д���UIDʱ��POST�����������п��ܻᳬ��URL����
                        result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                        //��һ�δ��ݰ���Ҽ�¼�������
                        if (result == "true")
                        {
                            disBll.UpdateChanges();
                            obBll.UpdateChanges();

                            foreach (var m in umbrList)
                            {
                                umbrBll.Insert(m, false);
                            }
                            umbrBll.UpdateChanges();

                            //һ���£���������б�����ɿ�  
                            var bmUMBBll = new BaseBll<bmUserMaBi>();
                            var bmUMBSBll = new BaseBll<bmUserMaBiSettleRecord>();

                            //�ڶ���ͬ����ʼ��ͬ���û��Ҽ�¼���û��ҽ����¼���û��Ҽ�¼������ bmUserMBList��
                            //ȡ������δ����İ���Ҽ�¼
                            var nonSettleBMBList = umbrBll.All.Where(p => p.IsSettle == null || p.IsSettle == false);
                            if (nonSettleBMBList.Count() > 0)
                            {
                                //ȡ����ǰ�����û��İ����ֵ
                                //ȥ��������ȡ��Ҫ������û�ID
                                var userIdsList = new List<Guid>();
                                result = "";
                                dts = DateTime.Now.ToString();
                                strUrl = CFG.��վ���� + CFG.����ͬ��_�����������û�ID��;
                                var appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);

                                LogHelper.Write("ͬ��������Ϣ" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
                                result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");
                                if (!String.IsNullOrEmpty(result))
                                {
                                    LogHelper.Write("�л�ȡ���û�����", LogHelper.LogMessageType.Info);
                                    s = "";
                                    try { s = DecodeJson(result); }
                                    catch
                                    {
                                        s = "";
                                        LogHelper.Write("�����쳣", LogHelper.LogMessageType.Info);
                                    }
                                    if (!String.IsNullOrEmpty(s))
                                    {
                                        //�û�ID��
                                        var userIds = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                                        s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);

                                        if (!String.IsNullOrEmpty(userIds))
                                        {//��UserIdû��ϵ���Ͳ�ͬ��UserId
                                            userIds = Compression.DecompressString(userIds);
                                            userIdsList = JsonConvert.DeserializeObject<List<Guid>>(userIds);
                                        }
                                    }
                                }
                                //ȥ��������ȡ��Ҫ������û�ID����
                                if (userIdsList.Count() > 0)
                                {
                                    var bmNewUserMBList = new BaseBll<bmNewUserMB>().All.Where(p => userIdsList.Contains(p.UserId));
                                    //���е��û������ֵ�����û���Ҽ�¼
                                    var bmUserMBList = new List<bmUserMaBi>();
                                    var bmUserMaBiSettleRecordList = new List<bmUserMaBiSettleRecord>();

                                    //����ʱ�� 
                                    var settleTime = DateTime.Now;
                                    foreach (var numb in bmNewUserMBList)
                                    {
                                        //��Ӱ���Ҽ�¼
                                        AddUserMB(mbRef, numb.NMB, bmUserMBList, settleTime, numb);
                                        AddUserMB(bbRef, numb.NBB, bmUserMBList, settleTime, numb);
                                        AddUserMB(banbRef, numb.NBANB, bmUserMBList, settleTime, numb);

                                        //��Ӱ���ҽ����¼
                                        AddUserMBSettle(mbRef, numb.NMB, bmUserMaBiSettleRecordList, settleTime, numb);
                                        AddUserMBSettle(bbRef, numb.NBB, bmUserMaBiSettleRecordList, settleTime, numb);
                                        AddUserMBSettle(banbRef, numb.NBANB, bmUserMaBiSettleRecordList, settleTime, numb);
                                    }

                                    //��δ����İ���ұ�ʶΪ����
                                    foreach (var um in nonSettleBMBList)
                                    {
                                        um.IsSettle = true;
                                    }

                                    //���ɴ��ݵ�JSON�б�
                                    var bmUserMBListJson = new List<bmUserMaBiJson>();
                                    var bmUserMaBiSettleRecordListJson = new List<bmUserMaBiSettleRecordJson>();
                                    //�û��İ���Ҽ�¼Json
                                    var nonSettleBMBListJson = new List<bmUserMaBiRecordJson>();
                                    //�û��İ���Ҽ�¼
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
                                    //�û��İ���ҽ����¼
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
                                    //����ҽ����¼JSON
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

                                    //����ҽ����������ͬ��
                                    s = "";
                                    if (bmUserMBListJson.Count() == 0)
                                    {
                                        s += " ";
                                    }
                                    else
                                    {
                                        s += ToJsonAndCompress(bmUserMBListJson);
                                    }
                                    s += CFG.������_JSON���ݼ��;

                                    if (bmUserMaBiSettleRecordListJson.Count() == 0)
                                    {
                                        s += " ";
                                    }
                                    else
                                    {
                                        s += ToJsonAndCompress(bmUserMaBiSettleRecordListJson);
                                    }
                                    s += CFG.������_JSON���ݼ��;

                                    if (nonSettleBMBListJson.Count() == 0)
                                    {
                                        s += " ";
                                    }
                                    else
                                    {
                                        s += ToJsonAndCompress(nonSettleBMBListJson);
                                    }
                                    s += CFG.������_JSON���ݼ��;

                                    //�������ͬ����ҡ�������������������
                                    result = "";
                                    dts = DateTime.Now.ToString();
                                    strUrl = CFG.��վ���� + CFG.����ͬ��_����ҽ���;

                                    //δEncode��URL
                                    neAppentUrl = "?tok=" + tok;
                                    if (!string.IsNullOrEmpty(s))
                                    {
                                        neAppentUrl += "&AncyData=" + SecurityHelper.Encrypt(s);
                                    }

                                    LogHelper.Write("ͬ���û��������Ϣ" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                                    //�д���UIDʱ��POST�����������п��ܻᳬ��URL����
                                    result = GetHtmlHelper.PostGetPage(strUrl, neAppentUrl.Substring(1), "");
                                    if (result == "true")
                                    {
                                        //var bmUserMBList = new List<bmUserMaBi>();
                                        //var bmUserMaBiSettleRecordList = new List<bmUserMaBiSettleRecord>();
                                        //�û������

                                        if (bmUserMBList.Count() > 0)
                                        {
                                            //ɾ�����е��û������
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
                                            LogHelper.Write("δ�����û�����ҽ����¼", LogHelper.LogMessageType.Info);
                                        }


                                        //�û�����ҽ����¼
                                        if (bmUserMaBiSettleRecordList.Count() > 0)
                                        {
                                            //ɾ��������ͬ���������û�����ҽ����¼
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
                                            LogHelper.Write("δ�����û�����ҽ����¼", LogHelper.LogMessageType.Info);

                                        }
                                        //ͳһ��������ݿ�
                                        bmUMBBll.UpdateChanges();
                                        bmUMBSBll.UpdateChanges();
                                        umbrBll.UpdateChanges();
                                    }//�ڶ�����ҽ���ͬ��
                                    else
                                    {
                                        LogHelper.Write("ͬ������ҽ�����Ϣʱδ������ȷ���" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                                    }
                                }//userIdsList.Count() > 0
                            }//nonSettleBMBList.Count() > 0
                        }//��һ�δ��ݰ���Ҽ�¼�������
                        else
                        {
                            LogHelper.Write("ͬ�����������Ϣʱδ������ȷ���" + strUrl + neAppentUrl, LogHelper.LogMessageType.Info);
                        }
                    }//else nonOperateOB.Count() > 0
                }//if (fiveAgoQADisWJS.Count() > 0)
            }//if ����������
        }

        /// <summary>
        /// �����û��İ����
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
                RegUser = Guid.Parse(CFG.���鴦���û�),
                RegTime = settleTime,
                ModTime = settleTime,
                FlagTrashed = false,
                FlagDeleted = false
            });
        }

        ///// <summary>
        ///// �û����Json ��¼���
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
        //        RegUser = Guid.Parse(CFG.���鴦���û�),
        //        RegTime = settleTime,
        //        ModTime = settleTime,
        //        FlagTrashed = false,
        //        FlagDeleted = false
        //    });
        //}
        /// <summary>
        /// �û�����ҽ����¼
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
                RegUser = Guid.Parse(CFG.���鴦���û�),
                RegTime = settleTime,
                ModTime = settleTime,
                FlagTrashed = false,
                FlagDeleted = false
            });
        }
        ///// <summary>
        ///// �û�����ҽ����¼��Json
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
        //        RegUser = Guid.Parse(CFG.���鴦���û�),
        //        RegTime = settleTime,
        //        ModTime = settleTime,
        //        FlagTrashed = false,
        //        FlagDeleted = false
        //    });
        //}

        /// <summary>
        /// ����ʱ���õ���Ӱ���ҷ��� 
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

            umbrModel.IsSettle = false; //ȡ�����ʱ������δ����ġ�ȡ��֮�󣬻���δ���㡣��ÿ���Զ�����ʱ�ٽ���
            umbrModel.RegTime = DateTime.Now;
            umbrModel.ModTime = DateTime.Now;
            umbrModel.FlagTrashed = false;
            umbrModel.FlagDeleted = false;
            umbrModel.RegUser = Guid.Parse(CFG.���鴦���û�);
            //���ݿ���Ҽ�¼���
            umbrList.Add(umbrModel);
            //Json����������
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
