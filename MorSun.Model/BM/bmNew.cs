﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HOHO18.Common;
using System.Data.Linq;
using System.ComponentModel.DataAnnotations;

namespace MorSun.Model
{
    [MetadataType(typeof(bmNewMetadata))]
    public partial class bmNew : IModel
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
            ParameterProcess.TrimParameter<bmNew>(this);            
            yield break;
        }

        partial void OnValidate(ChangeAction action)
        {
            if (!IsValid)
                throw new ApplicationException("Rule violations prevent saving");
        }
    }


    public class bmNewMetadata
    {
        [Display(Name = "新闻类别")]
        [Required(ErrorMessage = "{0}必选")]
        public System.Guid NewRef;
        [Display(Name = "标题")]
        [Required(ErrorMessage = "{0}必填")]
        public System.String NewTitle;
        [Display(Name = "内容")]
        [Required(ErrorMessage = "{0}必填")]
        public System.String NewContent;
        [Display(Name = "关键字")]        
        public System.String NewKeyWord;        
    }

}
