using HOHO18.Common;
using HOHO18.Common.WEB;
using Quartz;
using System;



namespace MorSun.Controllers.Quartz
{
    public class CheckingJob5:IJob
    {       
        public void Execute(IJobExecutionContext context)
        {
            LogHelper.Write("定时器开始执行用户数据同步", LogHelper.LogMessageType.Info);
            //如果是当天的2点到4点，获取昨天所有数据
            try
            {
                var beginTime = DateTime.Now.Date.AddHours(2);
                var endTime = DateTime.Now.Date.AddHours(4);
                LogHelper.Write("当天用户数据同步开始时间：" + beginTime.ToString() + "当天用户数据同步结束时间" + endTime.ToString(), LogHelper.LogMessageType.Debug);
                if (ChangeDateTime.IsInTime(beginTime, endTime))
                {//决定修改为三天内，防止网络断线
                    var ancyTime = DateTime.Now.Date.AddDays(-3).AddMinutes(-10);
                    LogHelper.Write("当天用户数据同步时间：" + ancyTime.ToString(), LogHelper.LogMessageType.Info);
                    new BasisController().AncyUser(ancyTime, "");
                }
                else
                {
                    new BasisController().AncyUser(null, "");
                }
            }
            catch
            {
                LogHelper.Write("定时器用户数据同步时出现异常", LogHelper.LogMessageType.Info);
            }
            //AncyUser(null, "");
        }
    }
}
