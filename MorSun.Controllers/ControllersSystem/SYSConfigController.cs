using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MorSun.Model;
using HOHO18.Common;
using MorSun.Bll;
using System.Collections;
using System.Web.Mvc;
using MorSun.Controllers.ViewModel;
using MorSun.Common.Privelege;
using MorSun.Common.常量集;
using MorSun.Common.类别;
using MorSun.Common.配置;
using HOHO18.Common.WEB;
using System.Configuration;
using System.Web.Configuration;
using MorSun.Controllers.Quartz;

namespace MorSun.Controllers.SystemController
{
    [HandleError]
    [Authorize]
    public class SYSConfigController : BaseController<bmOnlineQAUser>
    {
        protected override string ResourceId
        {
            get { return 资源.系统参数配置; }
        }

        #region 矿石代码
        /// <summary>
        /// 定时读取
        /// </summary>
        /// <returns></returns>
        public string dsdq()
        {
            //MorSunScheduler.Instance.Clear();
            //CheckingTrigger t = new CheckingTrigger();
            //t.Run();
            //CheckingTrigger2 t2 = new CheckingTrigger2();
            //t2.Run();
            //SimpleTriggerExample t3 = new SimpleTriggerExample();
            //t3.Run();
            //用户数据同步
            CheckingTrigger5 t5 = new CheckingTrigger5();
            t5.Run();
            return "true";
        }

        /// <summary>
        /// 是否开启状态
        /// </summary>
        /// <returns></returns>
        public string IsStart()
        {
            return MorSunScheduler.Instance.IsStart().ToString();
        }

        /// <summary>
        /// 清除矿石任务
        /// </summary>
        /// <returns></returns>
        public string Clear()
        {
            MorSunScheduler.Instance.Clear();
            return "true";
        }

        /// <summary>
        /// 停止矿石
        /// </summary>
        /// <returns></returns>
        public string Stop()
        {
            MorSunScheduler.Instance.Stop(false);
            return "true";
        }

        /// <summary>
        /// 开启矿石
        /// </summary>
        /// <returns></returns>
        public string Start()
        {
            MorSunScheduler.Instance.Start();
            return "true";
        }

        /// <summary>
        /// 全部继续
        /// </summary>
        /// <returns></returns>
        public string ResumeAll()
        {
            MorSunScheduler.Instance.ResumeAll();
            return "true";
        }

        /// <summary>
        /// 停止某项工作
        /// </summary>
        /// <returns></returns>
        public string StopJob(string name, string group)
        {
            MorSunScheduler.Instance.StopJob(name, group);
            return "true";
        }

        /// <summary>
        /// 触发某项工作
        /// </summary>
        /// <param name="name"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public string TriggerJob(string name, string group)
        {
            MorSunScheduler.Instance.TrggerJob(name, group);
            return "true";
        }
        #endregion

        #region webconfig加解密
        /// <summary>
        /// webconfig加密解密
        /// </summary>
        /// <returns></returns>
        public string ENCWeb()
        {
            if (!ResourceId.HP(操作.修改))
            {                
                var oper = new OperationResult(OperationResultType.Error, "无权限");
                oper.AppendData = ModelState.GE();
                return "无权限";
            }
            var provider = "RSAProtectedConfigurationProvider";
            var section = "connectionStrings";
            var section1 = "quartz";
            var section2 = "log4net";
            Configuration confg = WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath);
            ConfigurationSection configSect = confg.GetSection(section);
            if (configSect != null)
            {
                configSect.SectionInformation.ProtectSection(provider);
                confg.Save();
            }

            ConfigurationSection configSect1 = confg.GetSection(section1);
            if (configSect1 != null)
            {
                configSect1.SectionInformation.ProtectSection(provider);
                confg.Save();
            }

            ConfigurationSection configSect2 = confg.GetSection(section2);
            if (configSect2 != null)
            {
                configSect2.SectionInformation.ProtectSection(provider);
                confg.Save();
            }
            return "";
        }

        public string DECWeb()
        {
            if (!ResourceId.HP(操作.修改))
            {
                var oper = new OperationResult(OperationResultType.Error, "无权限");
                oper.AppendData = ModelState.GE();
                return "无权限";
            }
            var provider = "RSAProtectedConfigurationProvider";
            var section = "connectionStrings";
            var section1 = "quartz";
            var section2 = "log4net";
            Configuration config = WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath);
            ConfigurationSection configSect = config.GetSection(section);
            if (configSect.SectionInformation.IsProtected)
            {
                configSect.SectionInformation.UnprotectSection();
                config.Save();
            }

            ConfigurationSection configSect1 = config.GetSection(section1);
            if (configSect1.SectionInformation.IsProtected)
            {
                configSect1.SectionInformation.UnprotectSection();
                config.Save();
            }

            ConfigurationSection configSect2 = config.GetSection(section2);
            if (configSect2.SectionInformation.IsProtected)
            {
                configSect2.SectionInformation.UnprotectSection();
                config.Save();
            }
            return "";
        }
        #endregion
    }
}
