using HOHO18.Common;
using HOHO18.Common.WEB;
using Quartz;
using System;



namespace MorSun.Controllers.Quartz
{
    public class CheckingJob8:IJob
    {       
        public void Execute(IJobExecutionContext context)
        {
            LogHelper.Write("定时器开始执行用户邦马币结算", LogHelper.LogMessageType.Info);
            //如果是当天的2点到4点，获取昨天所有数据
            try
            {
                new BasisController().FinalAccount();                
            }
            catch
            {
                LogHelper.Write("定时器用户邦马币结算时出现异常", LogHelper.LogMessageType.Info);
            }
            //AncyUser(null, "");
        }
    }
}
