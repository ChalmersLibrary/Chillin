using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.datatype;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using System.Web.Security;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [Authorize]
    public class GetMemberSurfaceController : Controller
    {
        [HttpGet]
        public string GetMemberNameById(int memberId)
        {
            Member memberInfo = new Member(memberId);
            return memberInfo.LoginName;
        }
    }
}