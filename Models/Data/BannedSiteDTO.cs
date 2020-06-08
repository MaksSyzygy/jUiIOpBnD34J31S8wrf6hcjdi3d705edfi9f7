using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Checkitlink.Models.Data
{
    //список сайтов с которых закладку нельзя выложить в публичный доступ
    [Table("tblBannedSite")]
    public class BannedSiteDTO
    {
        [Key]
        public int SiteId { get; set; }
        public string SiteLink { get; set; }
    }
}