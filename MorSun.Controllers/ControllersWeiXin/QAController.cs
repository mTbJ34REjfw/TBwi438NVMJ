using MorSun.Common.配置;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MorSun.Controllers.ViewModel;

namespace MorSun.Controllers
{    
    [HandleError]
    public class QAController : Controller
    {
        public ActionResult Q(Guid? id)
        {
            var model = new BMQAVModel();
            if(id != null)
            {
                model.sId = id;
                return View(model);
            }    
            else
            {
                return RedirectToAction("Index", "Home");
            }
            
        }
        

        public ActionResult QAS(BMQAViewVModel model)
        {
            if(!string.IsNullOrEmpty(model.qaIds))
            {
                var qaids = model.qaIds.ToGuidList(",");
                model.SearchList = model.All.Where(p => qaids.Contains(p.ID));                
            }
            return View(model);
        }
    }
}
