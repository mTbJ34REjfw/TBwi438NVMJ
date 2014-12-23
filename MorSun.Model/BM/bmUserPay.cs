using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HOHO18.Common;
using System.Data.Linq;
using System.ComponentModel.DataAnnotations;

namespace MorSun.Model
{
    [MetadataType(typeof(bmUserPayMetadata))]
    public partial class bmUserPay : IModel
    {
        #region Extensibility Method Definitions
        partial void OnLoaded();
        partial void OnValidate(System.Data.Linq.ChangeAction action);
        partial void OnCreated();
        partial void OnParentIdChanging(Guid value);
        #endregion

        public string CheckedId { get; set; }
           
        public bool IsValid
        {
            get { return (GetRuleViolations().Count() == 0); }
        }

        public IEnumerable<RuleViolation> GetRuleViolations()
        {
            ParameterProcess.TrimParameter<bmUserPay>(this);            
            yield break;
        }

        partial void OnValidate(ChangeAction action)
        {
            if (!IsValid)
                throw new ApplicationException("Rule violations prevent saving");
        }
    }


    public class bmUserPayMetadata
    {
        [Display(Name = "用户ID")]
        [Required(ErrorMessage = "{0}必选")]
        public System.Guid UserId;
        [Display(Name = "真实姓名")]
        [Required(ErrorMessage = "{0}必填")]
        public System.String TrueName;
        [Display(Name = "支付宝账号")]
        [Required(ErrorMessage = "{0}必填")]
        public System.String ALiPayNum;
        [Display(Name = "开户银行")]
        public System.String BankRef;
        [Display(Name = "银行卡号")]
        public System.String BankNum;
        [Display(Name = "认证情况")]
        public System.String CertificationRef;
        [Display(Name = "备注")]
        public System.String Remark; 
    }

}
