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
        private static bmNewUserMB GetUserMaBiByUId(Guid userId)
        {
            //ȡ����ǰ�û��ѽ�������ֵ
            return new BaseBll<bmNewUserMB>().All.FirstOrDefault(p => p.UserId == userId);            
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
        protected static bool IsRZ(string tok, bool rz)
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
                {//����45����
                    rz = false;
                }
                else
                {
                    rz = true;
                    LogHelper.Write("ʱ�䣺" + ts.Substring(0, ind), LogHelper.LogMessageType.Info);
                }
            }
            catch
            {
                rz = false;
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
            var dts = DateTime.Now.ToString();//dt.ToShortDateString() + " " + dt.ToShortTimeString();            
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
                var s = DecodeJson(result);
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
            var dts = DateTime.Now.ToString();//dt.ToShortDateString() + " " + dt.ToShortTimeString();            
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
                var s = DecodeJson(result);
                if (!String.IsNullOrEmpty(s))
                {
                    //�û������ű�Ҫ�ȷֿ�
                    var bmQA = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmMB = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var qaDIS = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmOB = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmUW = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();
                    s = s.Substring(s.IndexOf(CFG.������_JSON���ݼ��) + CFG.������_JSON���ݼ��.Length);
                    var bmTK = s.Substring(0, s.IndexOf(CFG.������_JSON���ݼ��)).Trim();

                    var ubll = new BaseBll<aspnet_Users>();
                    var uids = new List<Guid>();
                    var bmQAJson = "";
                    var bmMBJson = "";
                    var qaDISJson = "";
                    var bmOBJson = "";
                    var bmUWJson = "";
                    var bmTKJson = "";
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

                    //userids ȥ�ظ�
                    uids = uids.Distinct().ToList();

                    //ͬ���û�ID ������Ѿ������������û�ID��Ҫͬ��
                    if (uids.Count() > 0)
                        UIDAncyUser(ubll, uids);

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
                                var bll = new BaseBll<bmQA>();
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
                                _list = _list.Where(p => aids.Contains(p.ID)).ToList();
                                foreach (var l in _list)
                                {
                                    bll.Insert(l, false);
                                }
                                bll.UpdateChanges();
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
                                var bll = new BaseBll<bmQADistribution>();
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
                                var bll = new BaseBll<bmUserWeixin>();
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
                    }
                    catch
                    {
                        LogHelper.Write("�û����ݻ�ȡ�쳣����ͬ������ʱһЩ����ͬ�����ɹ�", LogHelper.LogMessageType.Info);
                    }
                }                
                else
                {
                    LogHelper.Write("����ԭ��û�л�ȡ����������", LogHelper.LogMessageType.Info);
                }            
            }            
        }


        /// <summary>
        /// �û���ֵ����ͬ��
        /// </summary>
        /// <param name="SyncDT"></param>
        public void AncyRC()
        {
            var result = "";
            var dts = DateTime.Now.ToString();//dt.ToShortDateString() + " " + dt.ToShortTimeString();            
            var tok = SecurityHelper.Encrypt(dts + ";" + CFG.������_�Խ�ͳһ��);
            string strUrl = CFG.��վ���� + CFG.����ͬ��_��ҳ�ֵ��Ϣ;
            string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);            

            //LogHelper.Write("ͬ����ֵ��Ϣ" + strUrl + appendUrl, LogHelper.LogMessageType.Info);
            result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");

            //ͬ����ֵ��Ϣ            
            if (!String.IsNullOrEmpty(result))
            {
                LogHelper.Write("�л�ȡ����ֵ����", LogHelper.LogMessageType.Info);
                var s = DecodeJson(result);
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
