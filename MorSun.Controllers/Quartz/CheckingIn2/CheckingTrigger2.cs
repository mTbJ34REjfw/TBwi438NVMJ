﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common.Logging;
using Quartz.Impl;
using Quartz;

namespace MorSun.Controllers.Quartz.CheckingIn
{
    public class CheckingTrigger2
    {

        public virtual void Run()
        {
            ILog log = LogManager.GetLogger(typeof(CheckingTrigger));
            log.Info("------- Initializing -------------------");
            
            log.Info("------- Initialization Complete --------");

            log.Info("------- Scheduling Jobs ----------------");

            IJobDetail job = JobBuilder.Create<CheckingJob>()
                .WithIdentity("job31", "group31")//.RequestRecovery(true)//服务重启之后不用再执行任务 应用重启之后时候忽略过期任务，默认false
                .Build();
            
            ICronTrigger trigger = (ICronTrigger)TriggerBuilder.Create()
                                                      .WithIdentity("trigger31", "group31")
                                                      .WithCronSchedule("20 30 22 * * ?")//.WithCronSchedule("20 30 9,14,22 * * ?")
                                                      .Build();

            DateTimeOffset ft = MorSunScheduler.Instance.SchedulerJob(job, trigger);
            log.Info(job.Key + " has been scheduled to run at: " + ft
                     + " and repeat based on expression: "
                     + trigger.CronExpressionString);
        }
    }
}
