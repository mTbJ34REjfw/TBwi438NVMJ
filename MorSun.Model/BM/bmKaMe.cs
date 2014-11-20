using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HOHO18.Common;
using System.Data.Linq;
using System.ComponentModel.DataAnnotations;

namespace MorSun.Model
{
    [MetadataType(typeof(bmKaMeMetadata))]
    public partial class bmKaMe : IModel
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
            ParameterProcess.TrimParameter<bmKaMe>(this);            
            yield break;
        }

        partial void OnValidate(ChangeAction action)
        {
            if (!IsValid)
                throw new ApplicationException("Rule violations prevent saving");
        }
    }


    public class bmKaMeMetadata
    {
        [Display(Name = "卡密类别")]
        [Required(ErrorMessage = "{0}必选")]
        public System.Guid KaMeRef;
        [Display(Name = "卡密")]        
        public System.String KaMe;

        [Display(Name = "导入")]        
        public System.String ImportRef;
        [Display(Name = "充值")]
        public System.String Recharge;        
    }

}
