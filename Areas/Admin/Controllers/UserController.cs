using Checkitlink.Models.Data;
using Checkitlink.Models.ViewModels;
using PagedList;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Checkitlink.Areas.Admin.Controllers
{
    public class UserController : Controller
    {
        // GET: Admin/User
        //список всех юзеров сайта
        public async Task<ActionResult> Index(int? page)
        {
            string login = User.Identity.Name;

            if(login != "")
            {
                var pageNumber = page ?? 1;

                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersListDTO = await profile.Users.ToListAsync()
                    };

                    var modelVM = new ProfileVM()
                    {
                        UsersList = modelDTO.UsersListDTO.ToArray().Where(x => x.BanStatus == false).OrderBy(x => x.Login).Select(x => new UserVM(x)).ToList()
                    };

                    var usersOnPage = modelVM.UsersList.ToPagedList(pageNumber, 20);
                    ViewBag.usersOnPage = usersOnPage;

                    return View(modelVM);
                }
            }
            else
            {
                return RedirectToAction("Error");
            }
        }

        public async Task<ActionResult> AsyncQuery(string sort, string filter, string title, int? page)
        {
            var model = await this.UsersQueryPartial(sort, filter, title, page);

            if(title != "Черный список")//лавирование по страницам
            {
                return PartialView("_UsersQueryPartial", model);
            }
            else
            {
                return PartialView("_BlackListPartial", model);
            }
        }

        //сортировка и поиск
        public async Task<ProfileVM> UsersQueryPartial(string sort, string filter, string title, int? page)
        {
            ProfileVM profileVM = new ProfileVM();

            var pageNumber = page ?? 1;

            using(var profile = new ChekitDB())
            {
                string SortQuery = string.IsNullOrEmpty(sort) ? "login_asc" : sort;

                var modelDTO = new ProfileDTO()
                {
                    UsersListDTO = await profile.Users.ToListAsync(),
                    ListBlackListUsersDTO = await profile.BlackList.ToListAsync()
                };

                //сортировка по роли/кол-ву закладок
                switch (SortQuery)
                {
                    case "login_asc":
                        profileVM.UsersList = modelDTO.UsersListDTO.ToArray().Where(x => x.BanStatus == false).OrderBy(x => x.Login).Select(x => new UserVM(x)).ToList();
                        break;
                    case "user_admin":
                        profileVM.UsersList = modelDTO.UsersListDTO.ToArray().Where(x => x.BanStatus == false && x.Role == "Админ").OrderBy(x => x.Login).Select(x => new UserVM(x)).ToList();
                        break;
                    case "user_user":
                        profileVM.UsersList = modelDTO.UsersListDTO.ToArray().Where(x => x.BanStatus == false && x.Role == "Пользователь").OrderBy(x => x.Login).Select(x => new UserVM(x)).ToList();
                        break;
                    case "linksCount_asc":
                        profileVM.UsersList = modelDTO.UsersListDTO.ToArray().Where(x => x.BanStatus == false).OrderBy(x => x.LinksCount).Select(x => new UserVM(x)).ToList();
                        break;
                    case "linksCount_desc":
                        profileVM.UsersList = modelDTO.UsersListDTO.ToArray().Where(x => x.BanStatus == false).OrderByDescending(x => x.LinksCount).Select(x => new UserVM(x)).ToList();
                        break;
                    default:
                        profileVM.UsersList = modelDTO.UsersListDTO.ToArray().Where(x => x.BanStatus == false).OrderBy(x => x.Login).Select(x => new UserVM(x)).ToList();
                        break;
                }

                if(filter != null)
                {
                    if(title != "Черный список")//поиск по активным юзерам
                    {
                        profileVM.UsersList = profileVM.UsersList.Where(x => x.SearchInfo().ToLower().Contains(filter.ToLower())).ToList();
                    }
                    else//по заблокированным
                    {
                        List<UserVM> usersBlackList = modelDTO.UsersListDTO.Where(x => x.BanStatus == true).OrderBy(x => x.Login).Select(x => new UserVM(x)).ToList();
                        profileVM.UsersList = usersBlackList.Where(x => x.SearchInfo().ToLower().Contains(filter.ToLower())).ToList();
                        profileVM.ListBlackListUsersVM = modelDTO.ListBlackListUsersDTO.OrderBy(x => x.Login).Select(x => new BlackListVM(x)).ToList();
                    }
                }

                var usersOnPage = profileVM.UsersList.ToPagedList(pageNumber, 20);
                ViewBag.usersOnPage = usersOnPage;

                return profileVM;
            }
        }

        //частисное представление логина админа в углу экрана
        public ActionResult AdminLoginPartial()
        {
            string login = User.Identity.Name;

            if(login != "")
            {
                LoginPartialVM loginPartial;

                using (ChekitDB chekitDB = new ChekitDB())
                {
                    UsersDTO usersDTO = chekitDB.Users.FirstOrDefault(x => x.Login == login);

                    loginPartial = new LoginPartialVM()
                    {
                        Login = usersDTO.Login,
                        Avatar = usersDTO.AvatarName,
                        UserId = usersDTO.UserId
                    };

                    return PartialView("_AdminLoginPartial", loginPartial);
                }
            }
            else
            {
                return RedirectToAction("Error");
            }
        }

        //инфо про юзера
        [HttpGet]
        public async Task<ActionResult> InfoUser(int id)
        {
            ProfileVM profileVM = new ProfileVM();

            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.UserId == id),
                    AllLinks = await profile.Links.ToListAsync()
                };

                var modelVM = new ProfileVM()
                {
                    AllUserLinks = modelDTO.AllLinks.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToList()
                };

                profileVM.AllUserLinks = modelVM.AllUserLinks;

                profileVM.UserInfo = new UserVM(modelDTO.UsersProfileDTO);

                return View(profileVM);
            }
        }

        //инфо про админа
        public async Task<ActionResult> AdminDetails()
        {
            UserVM userVM;

            string login = User.Identity.Name;

            using (ChekitDB chekitDB = new ChekitDB())
            {
                UsersDTO usersDTO = await chekitDB.Users.FirstOrDefaultAsync(x => x.Login == login);

                userVM = new UserVM(usersDTO);
            }

            return View(userVM);
        }

        //получаем профиль юзера со списком категорий
        public async Task<ActionResult> EditUserCategory(int id)
        {
            ProfileVM profileVM = new ProfileVM();

            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.UserId == id),
                    CategoryProfileListDTO = await profile.Categories.ToListAsync()
                };

                profileVM.UserInfo = new UserVM(modelDTO.UsersProfileDTO);

                profileVM.CategoryProfileList = modelDTO.CategoryProfileListDTO.Where(x => x.UserCategory == id).OrderBy(x => x.CategoryName).Select(x => new CategoryVM(x)).ToList();

                return View(profileVM);
            }
        }

        //переименовываем выбранную категорию
        public async Task<ActionResult> RenameCategory(ProfileVM profileVM, int catId, string catName)
        {
            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UserCategoryDTO = await profile.Categories.FirstOrDefaultAsync(x => x.CategoryId == catId),
                    LinkListUserProfileDTO = await profile.Links.ToListAsync()
                };

                //и вместе с ней переименовываем закладки в этой категории
                foreach(var item in modelDTO.LinkListUserProfileDTO.Where(x => x.LinkCategoryId == catId).ToList())
                {
                    item.LinkCategory = catName;
                }

                modelDTO.UserCategoryDTO.CategoryName = catName;

                await profile.SaveChangesAsync();
            }

            TempData["OK"] = "Имя категории успешно изменено";

            return RedirectToAction("EditUserCategory", new { id = profileVM.UserInfo.UserId });
        }

        //удаление категории
        public async Task<ActionResult> DeleteCategory(ProfileVM profileVM, int deleteCatId)
        {
            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.UserId == profileVM.UserInfo.UserId),
                    UserCategoryDTO = await profile.Categories.FirstOrDefaultAsync(x => x.CategoryId == deleteCatId),
                    LinkListUserProfileDTO = await profile.Links.ToListAsync()
                };

                if(modelDTO.LinkListUserProfileDTO.Where(x=>x.LinkCategoryId == deleteCatId).Any())
                {
                    foreach (var item in modelDTO.LinkListUserProfileDTO.Where(x => x.LinkCategoryId == deleteCatId).ToList())
                    {
                        profile.Links.Remove(item);

                        //уменьшаем кол-во ссылок юзера
                        modelDTO.UsersProfileDTO.LinksCount -= 1;

                        string linkAvatar = Request.MapPath("~/Screenshots/Uploads/LinkAvatars/" + $"{item.LinkID.ToString()}/" + item.LinkPicture);

                        string linkDirectory = Request.MapPath("~/Screenshots/Uploads/LinkAvatars/" + item.LinkID.ToString());

                        if (System.IO.File.Exists(linkAvatar))
                        {
                            System.IO.File.Delete(linkAvatar);//удаляем скриншот
                            Directory.Delete(linkDirectory);//удаляем папку
                        }
                        else//если скриншота нет, то удаляем только директорию
                        {
                            Directory.Delete(linkDirectory);
                        }
                    }
                }

                profile.Categories.Remove(modelDTO.UserCategoryDTO);

                await profile.SaveChangesAsync();
            }

            TempData["OK"] = "Категория и её ссылки удалены";

            return RedirectToAction("EditUserCategory", new { id = profileVM.UserInfo.UserId });
        }

        //блокировка юзера
        [HttpGet]
        public ActionResult BanUser(int id)
        {
            ProfileVM profileVM = new ProfileVM();

            using(ChekitDB chekitDB = new ChekitDB())
            {
                UsersDTO usersDTO = chekitDB.Users.FirstOrDefault(x => x.UserId == id);

                profileVM.UserVM = new UserProfileVM(usersDTO);
            }

            return PartialView("_AddBlackList", profileVM);
        }

        [HttpPost]
        public ActionResult BanUser(ProfileVM profileVM)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            using(ChekitDB chekitDB = new ChekitDB())
            {
                UsersDTO usersDTO = chekitDB.Users.FirstOrDefault(x => x.Login == profileVM.UserVM.Login);

                if(usersDTO.Role == "Админ")
                {
                    TempData["Error"] = "Администратора нельзя заблокировать";

                    return RedirectToAction("Index");
                }

                usersDTO.BanStatus = true;

                chekitDB.SaveChanges();

                BlackListDTO blackListDTO = new BlackListDTO();

                blackListDTO.Email = profileVM.UserVM.Email;
                blackListDTO.Login = profileVM.UserVM.Login;
                blackListDTO.UserId = profileVM.UserVM.UserId;
                blackListDTO.Reason = profileVM.BlackListUserVM.Reason;

                chekitDB.BlackList.Add(blackListDTO);

                chekitDB.SaveChanges();
            }

            TempData["OK"] = "Пользователь занесен в черный список";

            return RedirectToAction("Index");
        }

        //удаление юзера
        public async Task<ActionResult> DeleteUser(int id)
        {
            using(ChekitDB chekitDB = new ChekitDB())
            {
                //находим юзера
                UsersDTO usersDTO = await chekitDB.Users.FirstOrDefaultAsync(x => x.UserId == id);

                //готовим список закладок
                List<LinkDTO> linkDTO = await chekitDB.Links.ToListAsync();

                //кнопка удалить у админа имеет статус disabled. Если вручную в браузере убрать этот статус, то программа удалит админа
                //а это дополнительная защита, так сказать, от дурака
                if (usersDTO.Role == "Админ")
                {
                    TempData["Error"] = "Администратора нельзя удалить";

                    return RedirectToAction("Index");
                }

                //удаляю папку и аватар юзера
                string avatarUserDirectory = Request.MapPath("~/Avatars/Uploads/UserAvatars/" + $"{ id.ToString()}");

                string avatarUserImg = Request.MapPath("~/Avatars/Uploads/UserAvatars/" + $"{id.ToString()}/" + usersDTO.AvatarName);

                if (System.IO.File.Exists(avatarUserImg))
                {
                    System.IO.File.Delete(avatarUserImg);

                    Directory.Delete(avatarUserDirectory);
                }
                else//если аватар не был загружен, удаляем директорию
                {
                    Directory.Delete(avatarUserDirectory);
                }

                //подготавливаем список закладок юзера для удаления
                List<LinkVM> userLinks = linkDTO.Where(x => x.UserAuthorId == usersDTO.UserId).Select(x => new LinkVM(x)).ToList();

                foreach(var item in userLinks)
                {
                    //находим директорию скриншота закладки
                    string directoryImg = Request.MapPath("~/Screenshots/Uploads/LinkAvatars/" + $"{item.LinkPicture.Substring(0, item.LinkPicture.IndexOf('.'))}");

                    //аналогично с самим скриншотом
                    string pathImg = Request.MapPath("~/Screenshots/Uploads/LinkAvatars/" + $"{item.LinkPicture.Substring(0, item.LinkPicture.IndexOf('.'))}/" + item.LinkPicture);

                    if (System.IO.File.Exists(pathImg))//проверяем есть ли скриншот
                    {
                        System.IO.File.Delete(pathImg);//удаляем скриншот
                        Directory.Delete(directoryImg);//удаляем папку
                    }

                    //удаляем закладки
                    chekitDB.Links.Remove(linkDTO.FirstOrDefault(x=>x.LinkID == item.LinkID));
                }

                //удаляем юзера
                chekitDB.Users.Remove(usersDTO);

                //сохраняем изменения
                await chekitDB.SaveChangesAsync();
            }

            TempData["OK"] = "Пользователь удален";

            return RedirectToAction("Index");
        }

        //список заблокированных юзеров
        public async Task<ActionResult> BlackList(int? page)
        {
            ProfileVM profileVM = new ProfileVM();

            int pageNumber = page ?? 1;

            using (var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    ListBlackListUsersDTO = await profile.BlackList.ToListAsync(),
                    UsersListDTO = await profile.Users.ToListAsync()
                };

                var modelVM = new ProfileVM()
                {
                    ListBlackListUsersVM = modelDTO.ListBlackListUsersDTO.ToArray().OrderBy(x => x.Id).Select(x => new BlackListVM(x)).ToList(),
                    UsersList = modelDTO.UsersListDTO.Where(x => x.BanStatus == true).Select(x => new UserVM(x)).ToList()
                };

                var usersOnPage = modelVM.ListBlackListUsersVM.ToPagedList(pageNumber, 20);
                ViewBag.usersOnPage = usersOnPage;

                return View(modelVM);
            }
        }

        //разблокировка юзера
        public ActionResult Unblock(int id, int userId)
        {
            using (ChekitDB chekitDB = new ChekitDB())
            {
                BlackListDTO blackList = chekitDB.BlackList.Find(id);
                UsersDTO usersDTO = chekitDB.Users.Find(userId);

                usersDTO.BanStatus = false;

                chekitDB.BlackList.Remove(blackList);
                chekitDB.SaveChanges();
            }

            TempData["OK"] = "Пользователь восстановлен!";

            return RedirectToAction("Index");
        }

        //создание администратора
        [HttpGet]
        public ActionResult CreateAdminUser()
        {
            UserVM userVM = new UserVM();

            return View(userVM);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAdminUser(UserVM userVM, HttpPostedFileBase file)
        {
            if (!ModelState.IsValid)
            {
                return View(userVM);
            }

            int idForAvatar = 0;

            using(ChekitDB chekitDB = new ChekitDB())
            {
                if(await chekitDB.Users.AnyAsync(x=>x.Login == userVM.Login))
                {
                    ModelState.AddModelError("loginmatch", $"Логин {userVM.Login} занят");

                    return View(userVM);
                }
                else if(await chekitDB.Users.AnyAsync(x=>x.Email == userVM.Email))
                {
                    ModelState.AddModelError("emailmatch", $"Email {userVM.Email} занят");

                    return View(userVM);
                }

                UsersDTO usersDTO = new UsersDTO();

                usersDTO.Login = userVM.Login;
                usersDTO.Email = userVM.Email;
                usersDTO.Password = userVM.Password;
                usersDTO.BanStatus = false;
                usersDTO.LinksCount = 0;
                usersDTO.Role = "Админ";

                chekitDB.Users.Add(usersDTO);
                await chekitDB.SaveChangesAsync();

                int id = usersDTO.UserId;
                int roleId = 1;

                UserRoleDTO userRole = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = roleId
                };

                chekitDB.UserRoles.Add(userRole);
                await chekitDB.SaveChangesAsync();

                idForAvatar = usersDTO.UserId;
            }

            TempData["OK"] = "Админ создан";

            #region UploadAvatar

            var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Avatars\\Uploads"));

            var pathString1 = Path.Combine(originalDirectory.ToString(), "UserAvatars");
            var pathString2 = Path.Combine(originalDirectory.ToString(), "UserAvatars\\" + idForAvatar.ToString());
            var pathString3 = Path.Combine(originalDirectory.ToString(), "UserAvatars\\" + idForAvatar.ToString() + "\\Thumbs");
            var pathString4 = Path.Combine(originalDirectory.ToString(), "UserAvatars\\" + idForAvatar.ToString() + "\\Gallery");
            var pathString5 = Path.Combine(originalDirectory.ToString(), "UserAvatars\\" + idForAvatar.ToString() + "\\Gallery\\Thumbs");

            if (!Directory.Exists(pathString1))
            {
                Directory.CreateDirectory(pathString1);
            }

            if (!Directory.Exists(pathString2))
            {
                Directory.CreateDirectory(pathString2);
            }

            if (!Directory.Exists(pathString3))
            {
                Directory.CreateDirectory(pathString3);
            }

            if (!Directory.Exists(pathString4))
            {
                Directory.CreateDirectory(pathString4);
            }

            if (!Directory.Exists(pathString5))
            {
                Directory.CreateDirectory(pathString5);
            }

            string[] extensions = { "image/jpg", "image/jpeg", "image/gif", "image/png" };

            if(file != null && file.ContentLength > 0)
            {
                string extension = file.ContentType.ToLower();
                int counter = 0;

                foreach(var item in extensions)
                {
                    if(extension != item)
                    {
                        counter++;

                        if(counter == extensions.Length)
                        {
                            ModelState.AddModelError("errorFormat", "Неправильный формат файла");

                            return View(userVM);
                        }
                    }
                }

                string avatarName = file.FileName;

                using(ChekitDB chekitDB = new ChekitDB())
                {
                    UsersDTO usersDTO = await chekitDB.Users.FindAsync(idForAvatar);

                    usersDTO.AvatarName = avatarName;

                    await chekitDB.SaveChangesAsync();
                }

                var pathOriginalAvatar = string.Format($"{pathString2}\\{avatarName}");
                var pathLittleAvatar = string.Format($"{pathString3}\\{avatarName}");

                file.SaveAs(pathOriginalAvatar);

                WebImage littleAvatar = new WebImage(file.InputStream);
                littleAvatar.Resize(150, 150);
                littleAvatar.Save(pathLittleAvatar);
            }

            #endregion

            return RedirectToAction("Index");
        }

        //редактирование профиля админа
        [HttpGet]
        public async Task<ActionResult> EditUser(int id)
        {
            UserProfileVM userVM;

            using(ChekitDB chekitDB = new ChekitDB())
            {
                UsersDTO usersDTO = await chekitDB.Users.FindAsync(id);

                if(usersDTO == null)
                {
                    return View("Error");
                }

                userVM = new UserProfileVM(usersDTO);
            }

            return View(userVM);
        }

        [HttpPost]
        public async Task<ActionResult> EditUser(UserProfileVM userVM, HttpPostedFileBase file)
        {
            int userId = userVM.UserId;

            if (!ModelState.IsValid)
            {
                return View("Error");
            }

            using(ChekitDB chekitDB = new ChekitDB())
            {
                if(await chekitDB.Users.Where(x => x.UserId != userVM.UserId).AnyAsync(x => x.Login == userVM.Login))
                {
                    ModelState.AddModelError("loginmatch", $"Логин {userVM.Login} занят");

                    return View(userVM);
                }
                else if(await chekitDB.Users.Where(x=>x.UserId != userVM.UserId).AnyAsync(x=>x.Email == userVM.Email))
                {
                    ModelState.AddModelError("emailmatch", $"Email {userVM.Email} занят");

                    return View(userVM);
                }
            }

            using(ChekitDB chekitDB = new ChekitDB())
            {
                UsersDTO usersDTO = await chekitDB.Users.FindAsync(userId);

                usersDTO.Login = userVM.Login;
                usersDTO.Email = userVM.Email;
                usersDTO.Password = usersDTO.Password;
                usersDTO.AvatarName = userVM.AvatarName;

                await chekitDB.SaveChangesAsync();
            }

            TempData["OK"] = "Профиль отредактирован";

            #region Image Upload

            if(file != null && file.ContentLength > 0)
            {
                string extension = file.ContentType.ToLower();

                string[] extensions = { "image/jpg", "image/jpeg", "image/gif", "image/png" };
                int counter = 0;

                //Проверяем расширение файла
                foreach (var item in extensions)
                {
                    if (extension != item)
                    {
                        counter++;

                        if (counter == extensions.Length)
                        {
                            using (ChekitDB chekitDB = new ChekitDB())
                            {
                                ModelState.AddModelError("", "Изображение не было загружено - неправильный формат изображения");

                                return View(userVM);
                            }
                        }
                    }
                }

                var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Avatars\\Uploads"));

                var pathString1 = Path.Combine(originalDirectory.ToString(), "UserAvatars\\" + userId.ToString());
                var pathString2 = Path.Combine(originalDirectory.ToString(), "UserAvatars\\" + userId.ToString() + "\\Thumbs");

                DirectoryInfo directoryInfo1 = new DirectoryInfo(pathString1);
                DirectoryInfo directoryInfo2 = new DirectoryInfo(pathString2);

                foreach(var item in directoryInfo1.GetFiles())
                {
                    item.Delete();
                }

                foreach(var item in directoryInfo2.GetFiles())
                {
                    item.Delete();
                }

                string avatarName = file.FileName;

                using(ChekitDB chekitDB = new ChekitDB())
                {
                    UsersDTO usersDTO = await chekitDB.Users.FindAsync(userId);
                    usersDTO.AvatarName = avatarName;

                    await chekitDB.SaveChangesAsync();
                }

                var pathOriginalAvatar = string.Format($"{pathString1}\\{avatarName}");
                var pathLittleAvatar = string.Format($"{pathString2}\\{avatarName}");

                file.SaveAs(pathOriginalAvatar);

                WebImage littleAvatar = new WebImage(file.InputStream);
                littleAvatar.Resize(150, 150);
                littleAvatar.Save(pathLittleAvatar);
            }

            #endregion

            return RedirectToAction("Index");
        }

        //список пользователей для назначение админа
        public async Task<ActionResult> AssignAdminUser(int? page)
        {
            List<UserVM> users = new List<UserVM>();
            List<UsersDTO> usersDTO;

            int pageNumber = page ?? 1;

            using(ChekitDB chekitDB = new ChekitDB())
            {
                usersDTO = await chekitDB.Users.ToListAsync();
                users = usersDTO.ToArray().Where(x => x.BanStatus == false).OrderBy(x => x.Login).Select(x => new UserVM(x)).ToList();
            }

            var usersOnPage = users.ToPagedList(pageNumber, 20);
            ViewBag.usersOnPage = usersOnPage;

            return View(usersOnPage);
        }

        //выбор админа
        public ActionResult ChoiceAdmin(int id)
        {
            using(ChekitDB chekitDB = new ChekitDB())
            {
                string login = User.Identity.Name;

                UsersDTO userNow = chekitDB.Users.FirstOrDefault(x => x.Login == login);

                if(userNow.UserId == id)
                {
                    TempData["Error"] = "С себя снять роль админа нельзя";

                    return RedirectToAction("AssignAdminUser");
                }

                List<UserRoleDTO> userRoles = chekitDB.UserRoles.ToList();
                UserRoleDTO checkAdmin = chekitDB.UserRoles.FirstOrDefault(x => x.UserId == id);

                List<UserRoleDTO> adminUsers = userRoles.Where(x => x.RoleId == 1).ToList();

                foreach(var item in adminUsers)
                {
                    if (checkAdmin.UserId == item.UserId)
                    {
                        chekitDB.UserRoles.Remove(checkAdmin);

                        chekitDB.SaveChanges();

                        UserRoleDTO user = new UserRoleDTO()
                        {
                            UserId = id,
                            RoleId = 2
                        };

                        UsersDTO commonUser = chekitDB.Users.FirstOrDefault(x => x.UserId == id);

                        commonUser.Role = "Пользователь";

                        TempData["OK"] = "Роль админа снята";

                        chekitDB.UserRoles.Add(user);

                        chekitDB.SaveChanges();

                        return RedirectToAction("AssignAdminUser");
                    }
                }

                chekitDB.UserRoles.Remove(checkAdmin);

                chekitDB.SaveChanges();

                UserRoleDTO newAdmin = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 1
                };

                chekitDB.UserRoles.Add(newAdmin);

                UsersDTO usersDTO = chekitDB.Users.FirstOrDefault(x => x.UserId == id);

                usersDTO.Role = "Админ";

                chekitDB.SaveChanges();
            }

            TempData["OK"] = "Админ назначен";

            return RedirectToAction("AssignAdminUser");
        }

        //изменение пароля. Вынес в отдельное представление, ведь эта опция используется не очень часто
        [HttpGet]
        public async Task<ActionResult> EditPassword(int id)
        {
            ProfileVM profileVM = new ProfileVM();

            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.UserId == id)
                };

                profileVM.UserInfo = new UserVM(modelDTO.UsersProfileDTO);
            }

            return View(profileVM);
        }

        [HttpPost]
        public async Task<ActionResult> EditPassword(ProfileVM profileVM, string oldPass)
        {
            using(var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.UserId == profileVM.UserInfo.UserId)
                };

                if(oldPass != modelDTO.UsersProfileDTO.Password)
                {
                    ModelState.AddModelError("oldPassError", "Старый пароль указан не верно");

                    return View(profileVM);
                }

                modelDTO.UsersProfileDTO.Password = profileVM.UserInfo.Password;

                await profile.SaveChangesAsync();

                TempData["OK"] = "Пароль успешно изменен";

                return RedirectToAction("Index");
            }
        }

        //защита
        public ActionResult Error()
        {
            return View("ExitError");
        }
    }
}