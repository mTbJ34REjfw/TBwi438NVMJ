using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HOHO18.Common;
using System.Data.Linq;
using System.ComponentModel.DataAnnotations;

namespace MorSun.Model
{
    [MetadataType(typeof(bmTakeNowMetadata))]
    public partial class bmTakeNow : IModel
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
            ParameterProcess.TrimParameter<bmTakeNow>(this);            
            yield break;
        }

        partial void OnValidate(ChangeAction action)
        {
            if (!IsValid)
                throw new ApplicationException("Rule violations prevent saving");
        }
    }


    public class bmTakeNowMetadata
    {
        [Display(Name = "币值")]
        //[Required(ErrorMessage = "{0}必填")]
        public System.Decimal MaBiNum;
        [Display(Name = "有效性")]        
        public System.Guid Effective;
        [Display(Name = "取现情况")]        
        public System.Guid TakeRef;
        [Display(Name = "用户取现备注")]
        public System.String UserRemark;
        [Display(Name = "马子取现说明")]
        public System.String BMExplain;
        [Display(Name = "取现时间")]
        public System.String TakeTime;        
    }

}
