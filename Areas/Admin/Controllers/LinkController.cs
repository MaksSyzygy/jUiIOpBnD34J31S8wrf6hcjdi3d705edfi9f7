using Checkitlink.Models.Data;
using Checkitlink.Models.ViewModels;
using PagedList;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Checkitlink.Areas.Admin.Controllers
{
    public class LinkController : Controller
    {
        // GET: Admin/Link
        //список юзеров на странице админа
        public async Task<ActionResult> Index(int? page)
        {
            var pageNumber = page ?? 1;

            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    AllLinks = await profile.Links.ToListAsync()
                };

                var modelVM = new ProfileVM()
                {
                    AllLinks = modelDTO.AllLinks.OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 30)
                };

                var linksOnPage = modelVM.AllLinks;
                ViewBag.linksOnPage = linksOnPage;

                return View(modelVM);
            }
        }

        //информация о закладке
        public async Task<ActionResult> LinkInfo(int id)
        {
            LinkVM linkVM;

            using(ChekitDB chekitDB = new ChekitDB())
            {
                LinkDTO linkDTO = await chekitDB.Links.FindAsync(id);

                if(linkDTO == null)
                {
                    return View("Error");
                }

                linkVM = new LinkVM(linkDTO);
            }

            return View(linkVM);
        }

        //админ может удалить любую закладку
        public async Task<ActionResult> DeleteLink(int id)
        {
            using(ChekitDB chekitDB = new ChekitDB())
            {
                LinkDTO linkDTO = await chekitDB.Links.FindAsync(id);

                chekitDB.Links.Remove(linkDTO);

                await chekitDB.SaveChangesAsync();
            }

            TempData["OK"] = "Закладка удалена";

            return RedirectToAction("Index");
        }

        //поиск закладок
        public async Task<ActionResult> LinksSearch(string filter, int? page)
        {
            ProfileVM profileVM = new ProfileVM();

            int pageNumber = page ?? 1;

            using (var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    AllLinks = await profile.Links.ToListAsync()
                };

                var modelVM = new ProfileVM()
                {
                    AllUserLinks = modelDTO.AllLinks.OrderBy(x => x.LinkName).Select(x => new LinkVM(x)).ToList()
                };

                profileVM.AllUserLinks = modelVM.AllUserLinks.Where(x => x.SearchLink().ToLower().Contains(filter.ToLower())).ToList();

                var linksOnPage = modelVM.AllUserLinks.ToPagedList(pageNumber, 50);
                ViewBag.linksOnPage = linksOnPage;

                return PartialView("_AllLinksPartial", profileVM);
            }
        }

        //список ресурсов запрещенных для публикации в общем доступе
        public async Task<ActionResult> SiteBlackList()
        {
            ProfileVM profileVM = new ProfileVM();

            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    BannedSiteListDTO = await profile.BannedSiteDTO.ToListAsync()
                };

                profileVM.BannedSiteVM = modelDTO.BannedSiteListDTO.OrderBy(x => x.SiteId).Select(x => new BannedSiteVM(x)).ToList();

                return View(profileVM);
            }
        }

        //добавление ключевого слова
        public async Task<ActionResult> AddBlackListSite(string banWord)
        {
            ProfileVM profileVM = new ProfileVM();

            using (var profile = new ChekitDB())
            {
                BannedSiteDTO bannedSiteDTO = new BannedSiteDTO()
                {
                    SiteLink = banWord
                };

                profile.BannedSiteDTO.Add(bannedSiteDTO);

                await profile.SaveChangesAsync();

                var modelDTO = new ProfileDTO()
                {
                    BannedSiteListDTO = await profile.BannedSiteDTO.ToListAsync()
                };

                TempData["OK"] = "Имя ресурса добавлено";

                profileVM.BannedSiteVM = modelDTO.BannedSiteListDTO.OrderBy(x => x.SiteId).Select(x => new BannedSiteVM(x)).ToList();

                return RedirectToAction("SiteBlackList");
            }
        }

        //корректировка админом закладок
        [HttpGet]
        public async Task<ActionResult> EditUserLink(int id)
        {
            LinkVM linkVM;

            using (ChekitDB chekitDB = new ChekitDB())
            {
                LinkDTO linkDTO = await chekitDB.Links.FindAsync(id);

                if (linkDTO == null)
                {
                    return View("Error");
                }

                linkVM = new LinkVM(linkDTO);
            }

            return View(linkVM);
        }

        [HttpPost]
        public async Task<ActionResult> EditUserLink(LinkVM linkVM)
        {
            using(ChekitDB chekitDB = new ChekitDB())
            {
                LinkDTO linkDTO = await chekitDB.Links.FirstOrDefaultAsync(x => x.LinkID == linkVM.LinkID);

                linkDTO.LinkName = linkVM.LinkName;
                
                if(linkVM.PublicStatus == false)
                {
                    linkDTO.PublicStatus = false;
                }
                else
                {
                    linkDTO.PublicStatus = linkVM.PublicStatus;
                }
                linkDTO.LinkDescription = linkVM.LinkDescription;

                await chekitDB.SaveChangesAsync();

                TempData["OK"] = "Закладка отредактирована";

                return RedirectToAction("LinkInfo", new { id = linkVM.LinkID });
            }
        }
    }
}