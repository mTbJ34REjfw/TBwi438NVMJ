using HOHO18.Common;
using HOHO18.Common.WEB;
using Quartz;
using System;



namespace MorSun.Controllers.Quartz
{
    public class CheckingJob7:IJob
    {       
        public void Execute(IJobExecutionContext context)
        {
            //LogHelper.Write("定时器开始执行问题数据同步", LogHelper.LogMessageType.Info);
            //如果是当天的2点到4点，获取昨天所有数据
            try
            {          
                //获取马币充值信息
                new BasisController().AncyRC();
                //同步充值马币记录
                new BasisController().RCMB();
            }
            catch
            {
                LogHelper.Write("定时器问题数据同步时出现异常", LogHelper.LogMessageType.Info);
            }
            //AncyUser(null, "");
        }
    }
}
