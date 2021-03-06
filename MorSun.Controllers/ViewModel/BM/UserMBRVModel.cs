﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Data.Objects;
using MorSun.Model;
using System.Web.Mvc;

namespace MorSun.Controllers.ViewModel
{
    //[Bind]
    public class UserMBRVModel : BaseVModel<bmUserMaBiRecord>
    {

        /// <summary>
        /// 获取用户列表
        /// </summary>
        public override IQueryable<bmUserMaBiRecord> List
        {
            get
            {
                var l = All;
                if (sUserId.HasValue && sUserId != Guid.Empty)
                    l = l.Where(p => p.UserId == sUserId);
                if (sSource.HasValue && sSource != Guid.Empty)
                    l = l.Where(p => p.SourceRef == sSource);
                if (sStartTime.HasValue)
                    l = l.Where(p => p.RegTime >= sStartTime);
                if (sEndTime.HasValue)
                { 
                    var searchT = sEndTime.Value.Date.AddDays(1).AddSeconds(-1);
                    l = l.Where(p => p.RegTime <= searchT);
                }
                return l.OrderBy(p => p.RegTime);
            }
        }

        public bmNewUserMB UserBMB
        {
            get
            {
                return new BaseVModel<bmNewUserMB>().All.FirstOrDefault(p => p.UserId == sUserId);
            }
        }


        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        public Guid? sUserId { get; set; }

        public Guid? sSource { get; set; }

        public DateTime? sStartTime { get; set; }

        public DateTime? sEndTime { get; set; }

        /// <summary>
        /// 被选中的编号
        /// </summary>
        public virtual string CheckedId { get; set; }       
        //离职
        public virtual bool IsFlagTrashed { get; set; }
        //退休
        public virtual bool IsFlagDeleted { get; set; }        
    }
}
