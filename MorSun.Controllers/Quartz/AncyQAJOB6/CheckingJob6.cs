using HOHO18.Common;
using HOHO18.Common.WEB;
using Quartz;
using System;



namespace MorSun.Controllers.Quartz
{
    public class CheckingJob6:IJob
    {       
        public void Execute(IJobExecutionContext context)
        {
            LogHelper.Write("定时器开始执行问题数据同步", LogHelper.LogMessageType.Info);
            //如果是当天的2点到4点，获取昨天所有数据
            try
            {
                var beginTime = DateTime.Now.Date.AddHours(2);
                var endTime = DateTime.Now.Date.AddHours(2.5);
                LogHelper.Write("当天问题数据同步开始时间：" + beginTime.ToString() + "当天问题数据同步结束时间" + endTime.ToString(), LogHelper.LogMessageType.Debug);
                if (ChangeDateTime.IsInTime(beginTime, endTime))
                {
                    var ancyTime = DateTime.Now.Date.AddDays(-3).AddMinutes(-10);//获取用户与问题，在凌晨2小时左右，获取三天内的数据，防止因为网络问题丢失部分数据
                    LogHelper.Write("当天问题数据同步时间：" + ancyTime.ToString(), LogHelper.LogMessageType.Info);
                    new BasisController().AncyQA(ancyTime);
                }
                else
                {
                    new BasisController().AncyQA(null);
                    new BasisController().SuperviseBMB();
                }
            }
            catch
            {
                LogHelper.Write("定时器问题数据同步时出现异常", LogHelper.LogMessageType.Info);
            }
            //AncyUser(null, "");
        }
    }
}
