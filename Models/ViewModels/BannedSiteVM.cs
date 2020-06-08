using Checkitlink.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Checkitlink.Models.ViewModels
{
    public class BannedSiteVM
    {
        public int SiteId { get; set; }
        public string SiteLink { get; set; }

        public BannedSiteVM(BannedSiteDTO row)
        {
            SiteId = row.SiteId;
            SiteLink = row.SiteLink;
        }

        public BannedSiteVM() { }
    }
}