﻿using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MorSun.Controllers.Quartz
{
    public class CheckingTrigger5
    {
        public virtual void Run()
        {            

            IJobDetail job = JobBuilder.Create<CheckingJob5>()
                .WithIdentity("job55", "group55")//.RequestRecovery(true)//服务重启之后不用再执行任务 应用重启之后时候忽略过期任务，默认false
                .Build();

            ICronTrigger trigger = (ICronTrigger)TriggerBuilder.Create()
                                                      .WithIdentity("trigger55", "group55")
                                                      .WithCronSchedule("5 3 * * * ?")//.WithCronSchedule("20 30 9,14,22 * * ?")
                                                      .Build();

            DateTimeOffset ft = MorSunScheduler.Instance.SchedulerJob(job, trigger);            
        }
    }
}
