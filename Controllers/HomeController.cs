using Checkitlink.Models.Data;
using Checkitlink.Models.ViewModels;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using HtmlAgilityPack;
using System.Net;
using System.Text;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Drawing;
using PagedList;
using System.Collections.Generic;

namespace Checkitlink.Controllers
{
    public class HomeController : Controller
    {
        //Точка входа
        public ActionResult Index()
        {
            return RedirectToAction("Landing");
        }

        //Главная страница
        [HttpGet]
        public ActionResult Landing()
        {
            string login = User.Identity.Name;

            if (!string.IsNullOrEmpty(login))
            {
                return RedirectToAction("UserProfile");
            }

            return View();
        }

        //Авторизация
        [HttpPost]
        public async Task<ActionResult> Landing(ProfileVM profileVM)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("emptyField", "Введите данные");

                return View(profileVM);
            }

            bool isUser = false;

            using (ChekitDB chekitDB = new ChekitDB())
            {
                if (await chekitDB.BlackList.AnyAsync(x => x.Login == profileVM.LoginVM.UserName))
                {
                    ModelState.AddModelError("ban", $"Ваш аккаунт заблокирован, причина - {chekitDB.BlackList.Select(x => x.Reason).First()}");

                    TempData["Error"] = $"Ваш аккаунт заблокирован, причина - {chekitDB.BlackList.Select(x => x.Reason).First()}";

                    return View(profileVM);
                }
            }

            using (ChekitDB chekitDB = new ChekitDB())
            {
                if (await chekitDB.Users.AnyAsync(x => x.Login == profileVM.LoginVM.UserName && x.Password == profileVM.LoginVM.Password))
                {
                    isUser = true;
                }

                if (!isUser)
                {
                    ModelState.AddModelError("entryError", "Ошибка логина или пароля");

                    ViewBag.Enter = "Ошибка входа";

                    return View(profileVM);
                }
                else
                {
                    FormsAuthentication.SetAuthCookie(profileVM.LoginVM.UserName, profileVM.LoginVM.RememberMy);

                    return Redirect(FormsAuthentication.GetRedirectUrl(profileVM.LoginVM.UserName, profileVM.LoginVM.RememberMy));
                }
            }
        }

        //Вывод информации о пользователе в углу экрана
        public ActionResult LoginPartial()
        {
            string login = User.Identity.Name;

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

                return PartialView("_LoginPartial", loginPartial);
            }
        }

        //Выход из профиля
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Landing");
        }

        //Страница пользователя, в аргумент передаем favourite - Избранное, title - Название страницы, page - номер страинцы для пейджера
        public async Task<ActionResult> UserProfile(string favourite, string title, int? page)
        {
            string login = User.Identity.Name;//тут и далее получаем имя пользователя

            var pageNumber = page ?? 1;//тут и далее номер страницы для пейджера

            if (favourite == "false" && title == "Личный кабинет")
            {
                var modelVM = await LinkPage(false, "Личный кабинет", login, pageNumber);//передаем в метод параметры

                //тут и далее, защита - если пользователь открыл две страницы и на одной из них вышел из профиля, а на другой сделал
                //запрос к другой странице или действие, то вывожу страницу ошибки для предотвращения
                //желтого экрана смерти
                if (modelVM.CategoryProfileList == null)
                {
                    return RedirectToAction("Error");
                }
                else
                {
                    return View(modelVM);
                }
            }
            else if(favourite == "true" && title == "Избранные закладки")
            {
                var modelVM = await LinkPage(true, "Избранные закладки", login, pageNumber);

                if (modelVM.CategoryProfileList == null)
                {
                    return RedirectToAction("Error");
                }
                else
                {
                    return View(modelVM);
                }
            }
            else if(favourite == "false" && title == "Общая лента публичных закладок")
            {
                var modelVM = await LinkPage(false, "Общая лента публичных закладок", login, pageNumber);

                if (modelVM.UsersLeadList == null)
                {
                    return RedirectToAction("Error");
                }
                else
                {
                    return View(modelVM);
                }
            }
            else
            {
                var modelVM = await LinkPage(false, "Личный кабинет", login, pageNumber);

                if(modelVM.CategoryProfileList == null)
                {
                    return RedirectToAction("Error");
                }
                else
                {
                    return View(modelVM);
                }
            }
        }

        private async Task<ProfileVM> LinkPage(bool favourite, string title, string login, int pageNumber)
        {
            ProfileVM modelVM = new ProfileVM();

            try
            {
                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),//находим юзера
                        CategoryProfileListDTO = await profile.Categories.ToListAsync(),//список всех категорий закладок
                        LinkListUserProfileDTO = await profile.Links.ToListAsync(),//список всех закладок
                        UsersListDTO = await profile.Users.ToListAsync()//список пользователей
                    };

                    if (favourite == false && title == "Личный кабинет")//для страницы Личный кабинет
                    {
                        var model = new ProfileVM()
                        {
                            //выводим список категорий пользователя и дефолтные категории "Все ссылки" и "Сохраненные"
                            CategoryProfileList = modelDTO.CategoryProfileListDTO.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки" || x.CategoryName == "Сохраненные закладки").ToArray().OrderBy(x => x.CategoryName).Select(x => new CategoryVM(x)).ToList(),
                            //выводим список закладок пользователя
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                            //список для счетчика закладок по категориям
                            AllUserLinks = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToList(),
                            //выпадающий список с категориями для создания новой закладки
                            CategoryList = new SelectList(modelDTO.CategoryProfileListDTO.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToArray().OrderBy(x => x.CategoryName).Select(x => new CategoryVM(x)).ToList(), "CategoryId", "CategoryName")
                        };

                        modelVM = model;
                    }
                    else if (favourite == false && title == "Общая лента публичных закладок")//для страницы "Общая лента публичных закладок"
                    {
                        var model = new ProfileVM()
                        {
                            //список всех пользователей
                            UsersLeadList = modelDTO.UsersListDTO.Where(x => x.BanStatus == false).ToArray().Select(x => new UserVM(x)).ToPagedList(pageNumber, 30),
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.PublicStatus == true && x.LinkCategory != "Сохраненные закладки").ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                            CategoryList = new SelectList(modelDTO.CategoryProfileListDTO.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToArray().OrderBy(x => x.CategoryName).Select(x => new CategoryVM(x)).ToList(), "CategoryId", "CategoryName"),
                            //список для отображения в общем списке всех закладок. Закладки пользотеля будут выделены, 
                            //а закладки других юзеров можно будет сохранить в свой список
                            MyLinks = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).Select(x => new LinkVM(x)).ToList()
                        };

                        modelVM = model;
                    }
                    else//для страницы "Избранные закладки"
                    {
                        var model = new ProfileVM()
                        {
                            CategoryProfileList = modelDTO.CategoryProfileListDTO.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки" || x.CategoryName == "Сохраненные закладки").ToArray().OrderBy(x => x.CategoryName).Select(x => new CategoryVM(x)).ToList(),
                            //список закладок которые имеют статус Избранной
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId && x.FavoriteStatus == true).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                            //счетчик для кол-ва закладок по категориям
                            AllUserLinks = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId && x.FavoriteStatus == true).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToList(),
                            CategoryList = new SelectList(modelDTO.CategoryProfileListDTO.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToArray().OrderBy(x => x.CategoryName).Select(x => new CategoryVM(x)).ToList(), "CategoryId", "CategoryName")
                        };

                        modelVM = model;
                    }

                    ViewBag.Title = title;

                    ViewBag.linkOnPage = modelVM.LinkListUserProfile;//модель для пейджера

                    return modelVM;
                }
            }
            catch (Exception)
            {
                return modelVM;
            }
        }

        //сортировка и вывод закладок по конкретной категории, в аргумент передаем имя категории, тайтл страницы и № для пейджера
        public async Task<ActionResult> SortLinksCategory(string categoryName, string title, int? page)
        {
            var model = await CategoryLinks(categoryName, title, page);

            return PartialView("_UserProfilePartial", model);
        }

        public async Task<ProfileVM> CategoryLinks(string categoryName, string title, int? page)
        {
            ProfileVM profileVM = new ProfileVM();

            string login = User.Identity.Name;

            var pageNumber = page ?? 1;

            using (var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),
                    LinkListUserProfileDTO = await profile.Links.ToListAsync()
                };

                if(title == "Личный кабинет")//тут и далее сортировка по конкретной странице
                {
                    var modelVM = new ProfileVM()
                    {
                        LinkListUserProfile = categoryName == "Все ссылки" ?
                        modelDTO.LinkListUserProfileDTO.ToArray().Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToList().ToPagedList(pageNumber, 24) :
                        modelDTO.LinkListUserProfileDTO.Where(x => x.LinkCategory == categoryName && x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToList().ToPagedList(pageNumber, 24)
                    };

                    profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                    profileVM.LinkListUserProfile = modelVM.LinkListUserProfile;

                    ViewBag.linkOnPage = modelVM.LinkListUserProfile;

                    return profileVM;
                }
                else
                {
                    var modelVM = new ProfileVM()
                    {
                        LinkListUserProfile = categoryName == "Все ссылки" ?
                        modelDTO.LinkListUserProfileDTO.Where(x => x.FavoriteStatus == true && x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToList().ToPagedList(pageNumber, 24) :
                        modelDTO.LinkListUserProfileDTO.Where(x => x.LinkCategory == categoryName && x.FavoriteStatus == true && x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToList().ToPagedList(pageNumber, 24)
                    };

                    profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                    profileVM.LinkListUserProfile = modelVM.LinkListUserProfile;

                    ViewBag.linkOnPage = modelVM.LinkListUserProfile;

                    return profileVM;
                }
            }
        }

        //создание новой категории
        public async Task<ActionResult> CreateNewCategory(string newCategory)
        {
            string login = User.Identity.Name;

            try
            {
                using (ChekitDB chekitDB = new ChekitDB())
                {
                    UsersDTO usersDTO = await chekitDB.Users.FirstOrDefaultAsync(x => x.Login == login);

                    if (await chekitDB.Categories.Where(x => x.UserCategory == usersDTO.UserId).AnyAsync(x => x.CategoryName == newCategory))
                    {
                        TempData["Error"] = $"Категория с именем {newCategory} уже есть в Вашем списке";

                        return RedirectToAction("UserProfile");
                    }

                    CategoriesDTO categoriesDTO = new CategoriesDTO();

                    categoriesDTO.CategoryName = newCategory;
                    categoriesDTO.UserCategory = usersDTO.UserId;

                    chekitDB.Categories.Add(categoriesDTO);
                    await chekitDB.SaveChangesAsync();

                    TempData["OK"] = "Категория создана";

                    return RedirectToAction("UserProfile");
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //просмотр информации о своем профиле
        public async Task<ActionResult> UserDetails()
        {
            try
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
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //редактирование профиля
        [HttpGet]
        public async Task<ActionResult> EditProfile(int id)
        {
            try
            {
                UserProfileVM userVM;

                using (ChekitDB chekitDB = new ChekitDB())
                {
                    UsersDTO usersDTO = await chekitDB.Users.FindAsync(id);

                    userVM = new UserProfileVM(usersDTO);
                }

                return View(userVM);
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> EditProfile(UserProfileVM userVM, HttpPostedFileBase file)
        {
            int userId = userVM.UserId;

            if (!ModelState.IsValid)
            {
                return View("Error");
            }

            using (ChekitDB chekitDB = new ChekitDB())
            {
                if (await chekitDB.Users.Where(x => x.UserId != userVM.UserId).AnyAsync(x => x.Login == userVM.Login))
                {
                    ModelState.AddModelError("loginmatch", $"Логин {userVM.Login} занят");

                    return View(userVM);
                }
                else if (await chekitDB.Users.Where(x => x.UserId != userVM.UserId).AnyAsync(x => x.Email == userVM.Email))
                {
                    ModelState.AddModelError("emailmatch", $"Email {userVM.Email} занят");

                    return View(userVM);
                }
            }

            using (ChekitDB chekitDB = new ChekitDB())
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

            if (file != null && file.ContentLength > 0)
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

                DirectoryInfo directoryInfo1 = new DirectoryInfo(pathString1);

                foreach (var item in directoryInfo1.GetFiles())
                {
                    item.Delete();
                }

                string avatarName = file.FileName;

                using (ChekitDB chekitDB = new ChekitDB())
                {
                    UsersDTO usersDTO = await chekitDB.Users.FindAsync(userId);
                    usersDTO.AvatarName = avatarName;

                    await chekitDB.SaveChangesAsync();
                }

                var pathOriginalAvatar = string.Format($"{pathString1}\\{avatarName}");

                file.SaveAs(pathOriginalAvatar);
            }

            #endregion

            return RedirectToAction("UserProfile");
        }

        //изменение пароля. Вынес в отдельное представление, ведь эта опция используется не очень часто
        [HttpGet]
        public async Task<ActionResult> EditPassword(int id)
        {
            ProfileVM profileVM = new ProfileVM();

            using (var profile = new ChekitDB())
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
            using (var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.UserId == profileVM.UserInfo.UserId)
                };

                if (oldPass != modelDTO.UsersProfileDTO.Password)
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

        //создание нового аккаунта
        [HttpGet]
        public ActionResult CreateAccount()
        {
            UserVM userVM = new UserVM();

            return View(userVM);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAccount(ProfileVM profileVM, HttpPostedFileBase file)
        {
            if (!ModelState.IsValid)
            {
                return View(profileVM);
            }

            int idForAvatar = 0;

            using (ChekitDB chekitDB = new ChekitDB())
            {
                if (await chekitDB.Users.AnyAsync(x => x.Login == profileVM.UserInfo.Login))
                {
                    ModelState.AddModelError("loginExist", $"Логин {profileVM.UserInfo.Login} занят");

                    ViewBag.CreateAccountError = "Ошибка создания нового аккаунта";

                    return View("Landing", profileVM);
                }
                else if (await chekitDB.Users.AnyAsync(x => x.Email == profileVM.UserInfo.Email))
                {
                    ModelState.AddModelError("emailExist", "Этот адрес занят");

                    ViewBag.CreateAccountError = "Ошибка создания нового аккаунта";

                    return View("Landing", profileVM);
                }

                UsersDTO usersDTO = new UsersDTO();

                usersDTO.Login = profileVM.UserInfo.Login;//логин
                usersDTO.Email = profileVM.UserInfo.Email;//почта
                usersDTO.Password = profileVM.UserInfo.Password;//пароль
                usersDTO.BanStatus = false;//статус бана на аккаунте. Бан может выписать только админ
                usersDTO.LinksCount = 0;//счетчик кол-ва добавленных и сохраненных пользователем закладок
                usersDTO.Role = "Пользователь";//Для отображения информации в админ зоне. Ни на что не влияет

                chekitDB.Users.Add(usersDTO);
                await chekitDB.SaveChangesAsync();

                int id = usersDTO.UserId;
                int role = 2;

                UserRoleDTO userRole = new UserRoleDTO()//модель ролей, используется для аутентификации пользователя
                {
                    UserId = id,
                    RoleId = role
                };

                chekitDB.UserRoles.Add(userRole);
                await chekitDB.SaveChangesAsync();

                idForAvatar = usersDTO.UserId;
            }

            TempData["OK"] = "Аккаунт создан";

            #region UploadAvatar

            var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Avatars\\Uploads"));

            var pathString1 = Path.Combine(originalDirectory.ToString(), "UserAvatars");
            var pathString2 = Path.Combine(originalDirectory.ToString(), "UserAvatars\\" + idForAvatar.ToString());

            if (!Directory.Exists(pathString1))
            {
                Directory.CreateDirectory(pathString1);
            }

            if (!Directory.Exists(pathString2))
            {
                Directory.CreateDirectory(pathString2);
            }

            string[] extensions = { "image/jpg", "image/jpeg", "image/gif", "image/png" };

            if (file != null && file.ContentLength > 0)
            {
                string extension = file.ContentType.ToLower();
                int counter = 0;

                foreach (var item in extensions)
                {
                    if (extension != item)
                    {
                        counter++;

                        if (counter == extensions.Length)
                        {
                            ModelState.AddModelError("errorFormat", "Неправильный формат файла");

                            return View(profileVM);
                        }
                    }
                }

                string avatarName = file.FileName;

                using (ChekitDB chekitDB = new ChekitDB())
                {
                    UsersDTO usersDTO = await chekitDB.Users.FindAsync(idForAvatar);

                    usersDTO.AvatarName = avatarName;

                    await chekitDB.SaveChangesAsync();
                }

                var pathOriginalAvatar = string.Format($"{pathString2}\\{avatarName}");

                file.SaveAs(pathOriginalAvatar);
            }

            #endregion

            TempData["Enter"] = "Теперь введите учетные данные для входа на сайт";

            return RedirectToAction("Landing");
        }

        //добавление новой закладки
        public async Task<ActionResult> AddLink(ProfileVM profileVM)
        {
            int userId = 0;

            string linkName = null;

            string formatLinkName = null;

            int idScreenshot = 0;

            string login = User.Identity.Name;

            try
            {
                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login)
                    };

                    userId = modelDTO.UsersProfileDTO.UserId;
                }

                if (!ModelState.IsValid)
                {
                    return View("Error");
                }

                using (ChekitDB chekitDB = new ChekitDB())
                {
                    if (await chekitDB.Links.Where(x => x.UserAuthorId == userId).AnyAsync(x => x.LinkAddress == profileVM.UserLink.LinkAddress))
                    {
                        TempData["Error"] = "Закладка с таким адресом уже Вами создана";

                        return RedirectToAction("UserProfile");
                    }

                    LinkDTO linkDTO = new LinkDTO();

                    if (profileVM.UserLink.LinkName == null)
                    {
                        try//парсим title для имени закладки
                        {
                            HtmlDocument doc = new HtmlDocument();

                            var request = (HttpWebRequest)WebRequest.Create(profileVM.UserLink.LinkAddress);
                            request.Method = "GET";

                            using (var response = (HttpWebResponse)request.GetResponse())
                            {
                                using (var stream = response.GetResponseStream())
                                {
                                    doc.Load(stream, Encoding.GetEncoding("UTF-8"));
                                }
                            }

                            linkName = doc.DocumentNode.SelectSingleNode("//title").InnerText;

                            string[] newPictureName = linkName.Split(new string[] { "&mdash", "&ndash", "&", ";" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var item in newPictureName)
                            {
                                formatLinkName += item;
                            }

                            linkDTO.LinkName = formatLinkName;
                        }
                        catch (Exception)//с некоторыми проблемы...
                        {
                            return View("ErrorAddScreenshot");
                        }
                    }
                    else
                    {
                        if (await chekitDB.Links.Where(x => x.UserAuthorId == userId).AnyAsync(x => x.LinkName == profileVM.UserLink.LinkName))
                        {
                            TempData["Error"] = "Закладка с таким именем уже создана";

                            return RedirectToAction("UserProfile");
                        }

                        linkDTO.LinkName = profileVM.UserLink.LinkName;
                    }

                    linkDTO.LinkAddress = profileVM.UserLink.LinkAddress;

                    linkDTO.LinkCategoryId = profileVM.UserLink.LinkCategoryId;

                    CategoriesDTO categoriesDTO = await chekitDB.Categories.FirstOrDefaultAsync(x => x.CategoryId == profileVM.UserLink.LinkCategoryId);

                    linkDTO.LinkCategory = categoriesDTO.CategoryName;

                    linkDTO.CreatedAt = Convert.ToDateTime(DateTime.Now);

                    if (profileVM.UserLink.LinkDescription == null)//если поле Описание пустое, то описание принимает Имя закладки
                    {
                        linkDTO.LinkDescription = linkDTO.LinkName;
                    }
                    else
                    {
                        linkDTO.LinkDescription = profileVM.UserLink.LinkDescription;
                    }

                    linkDTO.UserAuthorId = userId;

                    linkDTO.UserAuthor = login;

                    linkDTO.PublicStatus = false;//публичность закладки - по умолчанию видимая только юзеру, но юзер может изменить это и она будет видна всем пользователям

                    linkDTO.FavoriteStatus = false;//статус избранной закладки

                    linkDTO.LikeCount = 0;//кол-во "лайков"

                    chekitDB.Links.Add(linkDTO);

                    await chekitDB.SaveChangesAsync();

                    UsersDTO usersDTO = await chekitDB.Users.FirstOrDefaultAsync(x => x.Login == login);

                    usersDTO.LinksCount += 1;//увеличиваем счетчик кол-ва закладок юзера

                    await chekitDB.SaveChangesAsync();

                    idScreenshot = linkDTO.LinkID;
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }

            var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Screenshots\\Uploads"));//метод создаст папку для загружаемых картинок

            var pathString1 = Path.Combine(originalDirectory.ToString(), "LinkAvatars");//создаем подпапки в созданной директории
            var pathString2 = Path.Combine(originalDirectory.ToString(), "LinkAvatars\\" + idScreenshot.ToString());

            if (!Directory.Exists(pathString1))
            {
                Directory.CreateDirectory(pathString1);
            }

            if (!Directory.Exists(pathString2))
            {
                Directory.CreateDirectory(pathString2);
            }

            string linkAvatarName = null;

            using (ChekitDB chekitDB = new ChekitDB())
            {
                LinkDTO linkDTO = await chekitDB.Links.FindAsync(idScreenshot);

                try
                {
                    linkAvatarName = linkDTO.LinkID.ToString();

                    linkAvatarName += ".png";//формируем имя и расширение картинки для закладки

                    linkDTO.LinkPicture = linkAvatarName;

                    var path = string.Format($"{pathString2}\\{linkAvatarName}");//создаем путь

                    ChromeOptions chromeOptions = new ChromeOptions();//запускаем скриншотер
                    chromeOptions.AddArgument("--headless");//без вывода процесса для пользователя

                    ChromeDriver chromeDriver = new ChromeDriver(chromeOptions);

                    chromeDriver.Manage().Window.Size = new Size(800, 600);//задаем разрешение

                    chromeDriver.Navigate().GoToUrl(profileVM.UserLink.LinkAddress);//задаем адрес
                    ITakesScreenshot screenshotDriver = chromeDriver as ITakesScreenshot;

                    Screenshot screenShot = screenshotDriver.GetScreenshot();
                    screenShot.SaveAsFile(path);//сохраняем скрин

                    chromeDriver.Close();//закрываем
                    chromeDriver.Quit();//выходим
                }
                catch (WebDriverException)//с некоторыми проблеммы...
                {
                    linkAvatarName = null;

                    linkDTO.LinkPicture = linkAvatarName;

                    TempData["Error"] = "С данного сайта невозможно сделать скриншот. Тысяча извинений";
                }

                await chekitDB.SaveChangesAsync();
            }

            TempData["OK"] = "Закладка создана!";

            return RedirectToAction("UserProfile");
        }

        //добавление лайков/дизлайков закладки
        public async Task<string> AddLike(string button, int linkId)
        {
            string login = User.Identity.Name;

            int likeCount = 0;

            using(ChekitDB chekitDB = new ChekitDB())
            {
                var UserModelDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await chekitDB.Users.FirstOrDefaultAsync(x => x.Login == login)
                };

                if (button == "RatingUp")//лайк
                {
                    var linkLike = await chekitDB.LinkLikeStatus.FirstOrDefaultAsync(x=>x.LinkId == linkId);

                    if(linkLike == null || linkLike.LikeAuthorId != UserModelDTO.UsersProfileDTO.UserId)//если закладка без голосов и юзер не голосовал за эту закладку
                    {
                        var LinkDTO = new ProfileDTO()
                        {
                            LikeStatusDTO = new LinkLikeStatusDTO()
                            {
                                LinkId = linkId,
                                LikeAuthorId = UserModelDTO.UsersProfileDTO.UserId,
                                LikeStatus = true
                            }
                        };

                        chekitDB.LinkLikeStatus.Add(LinkDTO.LikeStatusDTO);
                        await chekitDB.SaveChangesAsync();

                        LinkDTO linkDTO = await chekitDB.Links.FindAsync(LinkDTO.LikeStatusDTO.LinkId);
                        linkDTO.LikeCount += 1;

                        likeCount += linkDTO.LikeCount;

                        await chekitDB.SaveChangesAsync();
                    }
                    else if(linkLike.LikeAuthorId == UserModelDTO.UsersProfileDTO.UserId && linkLike.LikeStatus == false)//если с голосами, но этот пользователь не голосовал за закладку
                    {
                        linkLike.LinkId = linkId;
                        linkLike.LikeStatus = true;
                        linkLike.LikeAuthorId = UserModelDTO.UsersProfileDTO.UserId;

                        LinkDTO linkDTO = await chekitDB.Links.FindAsync(linkId);
                        linkDTO.LikeCount += 1;

                        likeCount += linkDTO.LikeCount;

                        await chekitDB.SaveChangesAsync();
                    }
                    else
                    {
                        LinkDTO linkDTO = await chekitDB.Links.FindAsync(linkId);

                        return linkDTO.LikeCount.ToString();
                    }
                }

                if (button == "RatingDown")//дизлайк, тут все также, только минусуем рейтинг
                {
                    var linkDislike = await chekitDB.LinkLikeStatus.FirstOrDefaultAsync(x=>x.LinkId == linkId);

                    if(linkDislike == null || linkDislike.LikeAuthorId != UserModelDTO.UsersProfileDTO.UserId)
                    {
                        var LinkDTO = new ProfileDTO()
                        {
                            LikeStatusDTO = new LinkLikeStatusDTO()
                            {
                                LinkId = linkId,
                                LikeAuthorId = UserModelDTO.UsersProfileDTO.UserId,
                                LikeStatus = false
                            }
                        };

                        chekitDB.LinkLikeStatus.Add(LinkDTO.LikeStatusDTO);
                        await chekitDB.SaveChangesAsync();

                        LinkDTO linkDTO = await chekitDB.Links.FindAsync(linkId);
                        linkDTO.LikeCount -= 1;

                        likeCount += linkDTO.LikeCount;

                        await chekitDB.SaveChangesAsync();
                    }
                    else if(linkDislike.LikeAuthorId == UserModelDTO.UsersProfileDTO.UserId && linkDislike.LikeStatus == true)
                    {
                        linkDislike.LinkId = linkId;
                        linkDislike.LikeStatus = false;
                        linkDislike.LikeAuthorId = UserModelDTO.UsersProfileDTO.UserId;

                        LinkDTO linkDTO = await chekitDB.Links.FindAsync(linkId);
                        linkDTO.LikeCount -= 1;

                        likeCount += linkDTO.LikeCount;

                        await chekitDB.SaveChangesAsync();
                    }
                    else
                    {
                        LinkDTO linkDTO = await chekitDB.Links.FindAsync(linkId);

                        return linkDTO.LikeCount.ToString();
                    }
                }
            }

            return likeCount.ToString();//возвращаем кол-во
        }

        //присваивание закладке избранного статуса
        public async Task<ActionResult> AddFavorite(bool favourite, int linkId, string title, int? page)
        {
            try
            {
                ProfileVM profileVM = new ProfileVM();

                string login = User.Identity.Name;

                var pageNumber = page ?? 1;

                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),//текущий юзер
                        LinkListUserProfileDTO = await profile.Links.ToListAsync(),//список закладок
                        SubscribeOnUserListDTO = await profile.SubscribeOnUserDTO.ToListAsync()//список подписчиков
                    };

                    var favoriteLink = await profile.Links.FirstOrDefaultAsync(x => x.LinkID == linkId);

                    if (favourite == false)//если не избранное
                    {
                        favoriteLink.FavoriteStatus = true;

                        await profile.SaveChangesAsync();
                    }
                    else//наоброт
                    {
                        favoriteLink.FavoriteStatus = false;

                        await profile.SaveChangesAsync();
                    }

                    if (title == "Личный кабинет")//возвращаем представление по странице
                    {
                        var modelVM = new ProfileVM()
                        {
                            //список закладок юзера
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                        };

                        //список категорий
                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        profileVM.LinkListUserProfile = modelVM.LinkListUserProfile;

                        ViewBag.Title = "Личный кабинет";

                        ViewBag.linkOnPage = modelVM.LinkListUserProfile;

                        return PartialView("_UserProfilePartial", profileVM);
                    }
                    else if (title == "Избранные закладки")
                    {
                        var modelVM = new ProfileVM()
                        {
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId && x.FavoriteStatus == true).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                        };

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        profileVM.LinkListUserProfile = modelVM.LinkListUserProfile;

                        ViewBag.Title = "Избранные закладки";

                        ViewBag.linkOnPage = modelVM.LinkListUserProfile;

                        return PartialView("_UserProfilePartial", profileVM);
                    }
                    else//это для страницы "Избранные пользователи", под-страница "Закладки избранных пользователей"
                    {
                        var modelVM = new ProfileVM()
                        {
                            //список подписчиков
                            SubscribeOnUserList = modelDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == modelDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList()
                        };

                        //формируем список публичных закладкок пользователей, на которые подписан юзер
                        var leadLinks = from link in modelDTO.LinkListUserProfileDTO
                                        join leadUser in modelVM.SubscribeOnUserList
                                        on link.UserAuthorId equals leadUser.LeadUser
                                        where link.PublicStatus == true
                                        select new
                                        {
                                            LinkID = link.LinkID,
                                            LinkName = link.LinkName,
                                            LinkAddress = link.LinkAddress,
                                            LinkPicture = link.LinkPicture,
                                            LinkCategoryId = link.LinkCategoryId,
                                            LinkCategory = link.LinkCategory,
                                            CreatedAt = link.CreatedAt,
                                            LinkDescription = link.LinkDescription,
                                            UserAuthorId = link.UserAuthorId,
                                            UserAuthor = link.UserAuthor,
                                            PublicStatus = link.PublicStatus,
                                            FavoriteStatus = link.FavoriteStatus,
                                            LikeCount = link.LikeCount
                                        };

                        List<LinkVM> list = new List<LinkVM>();

                        foreach (var item in leadLinks)
                        {
                            list.Add(new LinkVM()
                            {
                                LinkID = item.LinkID,
                                LinkName = item.LinkName,
                                LinkAddress = item.LinkAddress,
                                LinkPicture = item.LinkPicture,
                                LinkCategoryId = item.LinkCategoryId,
                                LinkCategory = item.LinkCategory,
                                CreatedAt = item.CreatedAt,
                                LinkDescription = item.LinkDescription,
                                UserAuthorId = item.UserAuthorId,
                                UserAuthor = item.UserAuthor,
                                PublicStatus = item.PublicStatus,
                                FavoriteStatus = item.FavoriteStatus,
                                LikeCount = item.LikeCount
                            });
                        }

                        profileVM.LinkListUserProfile = list.ToPagedList(pageNumber, 24);

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        ViewBag.Title = "Закладки избранных пользователей";

                        ViewBag.linkOnPage = profileVM.LinkListUserProfile;

                        return PartialView("_UsersPartial", profileVM);
                    }
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //закладка по умолчанию скрыта от других, но тут можно сделать так, что её видели другие
        //все по аналогии с методом выше AddFavorite
        public async Task<ActionResult> PrivateLink(bool status, int linkId, string title, int? page)
        {
            try
            {
                ProfileVM profileVM = new ProfileVM();

                string login = User.Identity.Name;

                bool siteStatus = true;

                var pageNumber = page ?? 1;

                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),
                        LinkListUserProfileDTO = await profile.Links.ToListAsync(),
                        BannedSiteListDTO = await profile.BannedSiteDTO.ToListAsync()//список ресурсов с которых нельзя выложить закладку в публичный доступ
                    };

                    var privateLink = await profile.Links.FirstOrDefaultAsync(x => x.LinkID == linkId);

                    if (status == false)
                    {
                        //проверка адреса ссылки чтоб общая лента не наполнилась закладками с порносайтов и прочих ресурсов
                        //разбиваем ссылку
                        string[] checkSite = privateLink.LinkAddress.Split(new char[] { ' ', '.', ',', '/', ':' }, StringSplitOptions.RemoveEmptyEntries);

                        //сверяем со стоп-словами из черного списка
                        foreach(var link in modelDTO.BannedSiteListDTO)
                        {
                            foreach(var checkLink in checkSite)
                            {
                                if(link.SiteLink == checkLink)
                                {
                                    TempData["Error"] = "Содержимое этого ресурса нельзя добавить в общий доступ";

                                    siteStatus = false;

                                    break;
                                }
                            }

                            if(siteStatus == false)
                            {
                                break;
                            }
                        }

                        if(siteStatus == true)
                        {
                            privateLink.PublicStatus = true;
                        }
                        else
                        {
                            privateLink.PublicStatus = false;
                        }

                        await profile.SaveChangesAsync();
                    }
                    else
                    {
                        privateLink.PublicStatus = false;

                        await profile.SaveChangesAsync();
                    }

                    if (title == "Личный кабинет")
                    {
                        var modelVM = new ProfileVM()
                        {
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                        };

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        profileVM.LinkListUserProfile = modelVM.LinkListUserProfile;

                        ViewBag.Title = "Личный кабинет";

                        ViewBag.linkOnPage = modelVM.LinkListUserProfile;

                        return PartialView("_UserProfilePartial", profileVM);
                    }
                    else
                    {
                        var modelVM = new ProfileVM()
                        {
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId && x.FavoriteStatus == true).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                        };

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        profileVM.LinkListUserProfile = modelVM.LinkListUserProfile;

                        ViewBag.Title = "Избранные закладки";

                        ViewBag.linkOnPage = modelVM.LinkListUserProfile;

                        return PartialView("_UserProfilePartial", profileVM);
                    }
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //редактируем закладку
        [HttpGet]
        public async Task<ActionResult> EditLink(int id)
        {
            try
            {
                string login = User.Identity.Name;

                ProfileVM profileVM = new ProfileVM();

                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),
                        UserLinkDTO = await profile.Links.FirstOrDefaultAsync(x => x.LinkID == id)
                    };

                    profileVM.UserLink = new LinkVM(modelDTO.UserLinkDTO);

                    profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");
                }

                return PartialView("_EditLinkPartial", profileVM);
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> EditLink(ProfileVM profileVM)
        {
            try
            {
                string login = User.Identity.Name;

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Выберите категорию и попробуйте снова";

                    return RedirectToAction("UserProfile");
                }

                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),
                        UserLinkDTO = await profile.Links.FindAsync(profileVM.UserLink.LinkID)
                    };

                    profileVM.UserLink.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                    if (await profile.Links.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId && x.LinkID != profileVM.UserLink.LinkID).AnyAsync(x => x.LinkName == profileVM.UserLink.LinkName))
                    {
                        TempData["Error"] = "Закладка с таким именем уже существует";

                        return RedirectToAction("UserProfile");
                    }

                    modelDTO.UserLinkDTO.LinkName = profileVM.UserLink.LinkName;
                    modelDTO.UserLinkDTO.LinkCategoryId = profileVM.UserLink.LinkCategoryId;
                    modelDTO.UserLinkDTO.LinkDescription = profileVM.UserLink.LinkDescription;

                    CategoriesDTO categoriesDTO = await profile.Categories.FirstOrDefaultAsync(x => x.CategoryId == profileVM.UserLink.LinkCategoryId);

                    modelDTO.UserLinkDTO.LinkCategory = categoriesDTO.CategoryName;

                    await profile.SaveChangesAsync();
                }

                TempData["OK"] = "Закладка отредактирована";

                return RedirectToAction("UserProfile");
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //получаем профиль юзера со списком категорий
        public async Task<ActionResult> EditMyCategory(int id)
        {
            ProfileVM profileVM = new ProfileVM();

            using (var profile = new ChekitDB())
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
            using (var profile = new ChekitDB())
            {
                var modelDTO = new ProfileDTO()
                {
                    UserCategoryDTO = await profile.Categories.FirstOrDefaultAsync(x => x.CategoryId == catId),
                    LinkListUserProfileDTO = await profile.Links.ToListAsync()
                };

                //и вместе с ней переименовываем закладки в этой категории
                foreach (var item in modelDTO.LinkListUserProfileDTO.Where(x => x.LinkCategoryId == catId).ToList())
                {
                    item.LinkCategory = catName;
                }

                modelDTO.UserCategoryDTO.CategoryName = catName;

                await profile.SaveChangesAsync();
            }

            TempData["OK"] = "Имя категории успешно изменено";

            return RedirectToAction("EditMyCategory", new { id = profileVM.UserInfo.UserId });
        }

        //удаляем категорию
        public async Task<ActionResult> DeleteCategory(ProfileVM profileVM, int deleteCatId)
        {
            string login = User.Identity.Name;

            if (login != "")
            {
                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.UserId == profileVM.UserInfo.UserId),//юзер
                        UserCategoryDTO = await profile.Categories.FirstOrDefaultAsync(x => x.CategoryId == deleteCatId),//его категории
                        LinkListUserProfileDTO = await profile.Links.ToListAsync()//список ссылок
                    };

                    if (modelDTO.LinkListUserProfileDTO.Where(x => x.LinkCategoryId == deleteCatId).Any())
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
            }
            else
            {
                return RedirectToAction("Error");
            }

            TempData["OK"] = "Категория и её ссылки удалены";

            return RedirectToAction("EditMyCategory", new { id = profileVM.UserInfo.UserId });
        }

        //удаляем закладку
        public async Task<ActionResult> DeleteLink(int id)
        {
            string login = User.Identity.Name;

            if(login != "")//защита от пользователей, которые сделали logout, а на другой странице делают действия
            {
                using (ChekitDB chekitDB = new ChekitDB())
                {
                    LinkDTO linkDTO = await chekitDB.Links.FindAsync(id);

                    UsersDTO usersDTO = await chekitDB.Users.FirstOrDefaultAsync(x => x.UserId == linkDTO.UserAuthorId);

                    usersDTO.LinksCount -= 1;

                    string directoryImg = Request.MapPath("~/Screenshots/Uploads/LinkAvatars/" + id.ToString());//папка с скриншотом

                    string pathImg = Request.MapPath("~/Screenshots/Uploads/LinkAvatars/" + $"{id.ToString()}/" +  linkDTO.LinkPicture);//скриншот

                    if (System.IO.File.Exists(pathImg))
                    {
                        System.IO.File.Delete(pathImg);//удаляем скриншот
                        Directory.Delete(directoryImg);//удаляем папку
                    }
                    else//если скриншота нет, то удаляем только директорию
                    {
                        Directory.Delete(directoryImg);
                    }

                    chekitDB.Links.Remove(linkDTO);

                    await chekitDB.SaveChangesAsync();
                }

                TempData["OK"] = "Закладка удалена";

                return RedirectToAction("UserProfile");
            }
            else
            {
                return RedirectToAction("Error");
            }
        }

        //поиск закладок
        public async Task<ActionResult> SearchLink(string filter, string title, int? page)
        {
            try
            {
                ProfileVM profileVM = new ProfileVM();

                string login = User.Identity.Name;

                int pageNumber = page ?? 1;

                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),
                        LinkListUserProfileDTO = await profile.Links.ToListAsync()
                    };

                    if (title == "Личный кабинет")//по страницам
                    {
                        var modelVM = new ProfileVM()
                        {
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24)
                        };

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        profileVM.LinkListUserProfile = modelVM.LinkListUserProfile.Where(x => x.SearchLink().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 24);

                        return PartialView("_UserProfilePartial", profileVM);
                    }
                    else if (title == "Избранные закладки")
                    {
                        var modelVM = new ProfileVM()
                        {
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId && x.FavoriteStatus == true).OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24)
                        };

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        profileVM.LinkListUserProfile = modelVM.LinkListUserProfile.Where(x => x.SearchLink().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 24);

                        return PartialView("_UserProfilePartial", profileVM);
                    }
                    else
                    {
                        var modelVM = new ProfileVM()
                        {
                            LinkListUserProfile = modelDTO.LinkListUserProfileDTO.Where(x => x.PublicStatus == true).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                            MyLinks = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).Select(x => new LinkVM(x)).ToList()
                        };

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        profileVM.LinkListUserProfile = modelVM.LinkListUserProfile.Where(x => x.SearchLink().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 24);

                        profileVM.MyLinks = modelVM.MyLinks;

                        return PartialView("_UserProfilePartial", profileVM);
                    }
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //для страницы "Избранные пользователи". 
        public async Task<ActionResult> Users(string title, string choiceUserListButton, string filter, int? page)
        {
            ProfileVM profileVM = new ProfileVM();

            int pageNumber = page ?? 1;

            string login = User.Identity.Name;

            if(choiceUserListButton != null)
            {
                title = choiceUserListButton;
            }

            using(var profile = new ChekitDB())
            {
                var userDTO = new ProfileDTO()
                {
                    UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),//юзер
                    SubscribeOnUserListDTO = await profile.SubscribeOnUserDTO.ToListAsync(),//список подписчиков
                    LinkListUserProfileDTO = await profile.Links.ToListAsync(),//список закладок
                    UsersListDTO = await profile.Users.ToListAsync()//список юзеров
                };

                if(choiceUserListButton == "Избранные пользователи")//пользователи на которых юзер подписался
                {
                    try
                    {
                        var modelSubscriber = new ProfileVM()
                        {
                            //готовим список из юзеров на которых подписался текущий юзер
                            SubscribeOnUserList = userDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == userDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList()
                        };

                        //формируем
                        var usersLead = from leadUser in userDTO.UsersListDTO
                                        join subscriber in modelSubscriber.SubscribeOnUserList
                                        on leadUser.UserId equals subscriber.LeadUser
                                        select new
                                        {
                                            UserId = leadUser.UserId,
                                            Login = leadUser.Login,
                                            Email = leadUser.Email,
                                            Password = leadUser.Password,
                                            AvatarName = leadUser.AvatarName,
                                            LinksCount = leadUser.LinksCount,
                                            BanStatus = leadUser.BanStatus,
                                            Role = leadUser.Role
                                        };

                        List<UserVM> list = new List<UserVM>();

                        foreach (var item in usersLead)
                        {
                            list.Add(new UserVM()
                            {
                                UserId = item.UserId,
                                Login = item.Login,
                                Email = item.Email,
                                Password = item.Password,
                                AvatarName = item.AvatarName,
                                LinksCount = item.LinksCount,
                                BanStatus = item.BanStatus,
                                Role = item.Role
                            });
                        }

                        //кол-во закладок пользователя на которого подписан текущий юзер
                        var userLeadLinkCount = from link in userDTO.LinkListUserProfileDTO
                                                join userLead in modelSubscriber.SubscribeOnUserList
                                                on link.UserAuthorId equals userLead.LeadUser
                                                select new
                                                {
                                                    LinkID = link.LinkID,
                                                    LinkName = link.LinkName,
                                                    LinkAddress = link.LinkAddress,
                                                    LinkPicture = link.LinkPicture,
                                                    LinkCategoryId = link.LinkCategoryId,
                                                    LinkCategory = link.LinkCategory,
                                                    CreatedAt = link.CreatedAt,
                                                    LinkDescription = link.LinkDescription,
                                                    UserAuthorId = link.UserAuthorId,
                                                    UserAuthor = link.UserAuthor,
                                                    PublicStatus = link.PublicStatus,
                                                    FavoriteStatus = link.FavoriteStatus,
                                                    LikeCount = link.LikeCount
                                                };

                        List<LinkVM> listLinkCount = new List<LinkVM>();

                        foreach (var item in userLeadLinkCount)
                        {
                            listLinkCount.Add(new LinkVM()
                            {
                                LinkID = item.LinkID,
                                LinkName = item.LinkName,
                                LinkAddress = item.LinkAddress,
                                LinkPicture = item.LinkPicture,
                                LinkCategoryId = item.LinkCategoryId,
                                LinkCategory = item.LinkCategory,
                                CreatedAt = item.CreatedAt,
                                LinkDescription = item.LinkDescription,
                                UserAuthorId = item.UserAuthorId,
                                UserAuthor = item.UserAuthor,
                                PublicStatus = item.PublicStatus,
                                FavoriteStatus = item.FavoriteStatus,
                                LikeCount = item.LikeCount
                            });
                        }

                        profileVM.AllUserLinks = listLinkCount.Where(x => x.PublicStatus == true).ToList();

                        profileVM.SubscribeOnUserList = modelSubscriber.SubscribeOnUserList;

                        ViewBag.Title = "Избранные пользователи";

                        ViewBag.UserType = "Избранные пользователи";

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == userDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        if (filter == null)//проверка строки поиска
                        {
                            profileVM.UsersLeadList = list.ToPagedList(pageNumber, 30);
                        }
                        else
                        {
                            profileVM.UsersLeadList = list.Where(x => x.SearchInfo().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 30);
                        }

                        ViewBag.usersOnPage = profileVM.UsersLeadList;

                        return PartialView("_UsersPartial", profileVM);
                    }
                    catch (Exception)
                    {
                        return RedirectToAction("Error");
                    }
                }
                else if(choiceUserListButton == "Подписаны на меня" || title == "Подписаны на меня")
                {
                    try
                    {
                        var modelSubscriber = new ProfileVM()
                        {
                            //тут аналогично примеру выше
                            SubscribeOnUserList = userDTO.SubscribeOnUserListDTO.Where(x => x.LeadUser == userDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList()
                        };

                        var modelMySubscriber = new ProfileVM()
                        {
                            //а тут формируем спаренный список - список юзеров на которых подписан текущий юзер, и список пользователей которые подписаны на ТЕКУЩЕГО пользователя
                            //это сделано для того, чтоб текущий юзер мог подписаться на пользователя который подписан на него, а благодаря этому списку выполняется
                            //проверка - подписан ли текущий юзер на того, кто на него подписался
                            SubscribeOnUserList = userDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == userDTO.UsersProfileDTO.UserId || x.LeadUser == userDTO.UsersProfileDTO.UserId).Select(x => new SubscribeOnUserVM(x)).ToList()
                        };

                        var usersLead = from leadUser in userDTO.UsersListDTO
                                        join subscriber in modelSubscriber.SubscribeOnUserList
                                        on leadUser.UserId equals subscriber.UserSubscriber
                                        select new
                                        {
                                            UserId = leadUser.UserId,
                                            Login = leadUser.Login,
                                            Email = leadUser.Email,
                                            Password = leadUser.Password,
                                            AvatarName = leadUser.AvatarName,
                                            LinksCount = leadUser.LinksCount,
                                            BanStatus = leadUser.BanStatus,
                                            Role = leadUser.Role
                                        };

                        List<UserVM> list = new List<UserVM>();

                        foreach (var item in usersLead)
                        {
                            list.Add(new UserVM()
                            {
                                UserId = item.UserId,
                                Login = item.Login,
                                Email = item.Email,
                                Password = item.Password,
                                AvatarName = item.AvatarName,
                                LinksCount = item.LinksCount,
                                BanStatus = item.BanStatus,
                                Role = item.Role
                            });
                        }

                        //счетчик закладок
                        var userLeadLinkCount = from link in userDTO.LinkListUserProfileDTO
                                                join userLead in modelSubscriber.SubscribeOnUserList
                                                on link.UserAuthorId equals userLead.UserSubscriber
                                                select new
                                                {
                                                    LinkID = link.LinkID,
                                                    LinkName = link.LinkName,
                                                    LinkAddress = link.LinkAddress,
                                                    LinkPicture = link.LinkPicture,
                                                    LinkCategoryId = link.LinkCategoryId,
                                                    LinkCategory = link.LinkCategory,
                                                    CreatedAt = link.CreatedAt,
                                                    LinkDescription = link.LinkDescription,
                                                    UserAuthorId = link.UserAuthorId,
                                                    UserAuthor = link.UserAuthor,
                                                    PublicStatus = link.PublicStatus,
                                                    FavoriteStatus = link.FavoriteStatus,
                                                    LikeCount = link.LikeCount
                                                };

                        List<LinkVM> listLinkCount = new List<LinkVM>();

                        foreach (var item in userLeadLinkCount)
                        {
                            listLinkCount.Add(new LinkVM()
                            {
                                LinkID = item.LinkID,
                                LinkName = item.LinkName,
                                LinkAddress = item.LinkAddress,
                                LinkPicture = item.LinkPicture,
                                LinkCategoryId = item.LinkCategoryId,
                                LinkCategory = item.LinkCategory,
                                CreatedAt = item.CreatedAt,
                                LinkDescription = item.LinkDescription,
                                UserAuthorId = item.UserAuthorId,
                                UserAuthor = item.UserAuthor,
                                PublicStatus = item.PublicStatus,
                                FavoriteStatus = item.FavoriteStatus,
                                LikeCount = item.LikeCount
                            });
                        }

                        profileVM.AllUserLinks = listLinkCount.Where(x => x.PublicStatus == true).ToList();

                        profileVM.SubscribeOnUserList = modelMySubscriber.SubscribeOnUserList;

                        ViewBag.Title = "Подписаны на меня";

                        ViewBag.UserType = "Подписаны на меня";

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == userDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        if (filter == null)
                        {
                            profileVM.UsersLeadList = list.ToPagedList(pageNumber, 30);
                        }
                        else
                        {
                            profileVM.UsersLeadList = list.Where(x => x.SearchInfo().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 30);
                        }

                        ViewBag.usersOnPage = profileVM.UsersLeadList;

                        return PartialView("_UsersPartial", profileVM);
                    }
                    catch (Exception)
                    {
                        return RedirectToAction("Error");
                    }
                }
                else if (choiceUserListButton == "Все пользователи" || title == "Все пользователи")
                {
                    try
                    {
                        var modelVM = new ProfileVM()
                        {
                            //проверка подписан ли текущий на кого-либо из общего списка всех пользователей
                            SubscribeOnUserList = userDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == userDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList(),

                            //список всех юзеров
                            UsersLeadList = userDTO.UsersListDTO.Where(x => x.BanStatus == false && x.UserId != userDTO.UsersProfileDTO.UserId).Select(x => new UserVM(x)).ToPagedList(pageNumber, 30)
                        };

                        var userLeadLinkCount = from link in userDTO.LinkListUserProfileDTO
                                                join userLead in modelVM.UsersLeadList
                                                on link.UserAuthorId equals userLead.UserId
                                                select new
                                                {
                                                    LinkID = link.LinkID,
                                                    LinkName = link.LinkName,
                                                    LinkAddress = link.LinkAddress,
                                                    LinkPicture = link.LinkPicture,
                                                    LinkCategoryId = link.LinkCategoryId,
                                                    LinkCategory = link.LinkCategory,
                                                    CreatedAt = link.CreatedAt,
                                                    LinkDescription = link.LinkDescription,
                                                    UserAuthorId = link.UserAuthorId,
                                                    UserAuthor = link.UserAuthor,
                                                    PublicStatus = link.PublicStatus,
                                                    FavoriteStatus = link.FavoriteStatus,
                                                    LikeCount = link.LikeCount
                                                };

                        List<LinkVM> listLinkCount = new List<LinkVM>();

                        foreach (var item in userLeadLinkCount)
                        {
                            listLinkCount.Add(new LinkVM()
                            {
                                LinkID = item.LinkID,
                                LinkName = item.LinkName,
                                LinkAddress = item.LinkAddress,
                                LinkPicture = item.LinkPicture,
                                LinkCategoryId = item.LinkCategoryId,
                                LinkCategory = item.LinkCategory,
                                CreatedAt = item.CreatedAt,
                                LinkDescription = item.LinkDescription,
                                UserAuthorId = item.UserAuthorId,
                                UserAuthor = item.UserAuthor,
                                PublicStatus = item.PublicStatus,
                                FavoriteStatus = item.FavoriteStatus,
                                LikeCount = item.LikeCount
                            });
                        }

                        profileVM.AllUserLinks = listLinkCount.Where(x => x.PublicStatus == true).ToList();

                        profileVM.SubscribeOnUserList = modelVM.SubscribeOnUserList;

                        ViewBag.Title = "Все пользователи";

                        ViewBag.UserType = "Все пользователи";

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == userDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        if (filter == null)
                        {
                            profileVM.UsersLeadList = modelVM.UsersLeadList;
                        }
                        else
                        {
                            profileVM.UsersLeadList = modelVM.UsersLeadList.Where(x => x.SearchInfo().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 24);
                        }

                        ViewBag.usersOnPage = profileVM.UsersLeadList;

                        return PartialView("_UsersPartial", profileVM);
                    }
                    catch (Exception)
                    {
                        return RedirectToAction("Error");
                    }
                }
                else if (choiceUserListButton == "Закладки избранных пользователей" || title == "Закладки избранных пользователей")
                {
                    try
                    {
                        var modelVM = new ProfileVM()
                        {
                            //список юзеров на которых подписался текущий
                            SubscribeOnUserList = userDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == userDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList(),

                            //исключаем из списка закладок закладки текущего
                            MyLinks = userDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == userDTO.UsersProfileDTO.UserId).Select(x => new LinkVM(x)).ToList()
                        };

                        //формируем список закладок юзеров
                        var leadLinks = from link in userDTO.LinkListUserProfileDTO
                                        join leadUser in modelVM.SubscribeOnUserList
                                        on link.UserAuthorId equals leadUser.LeadUser
                                        select new
                                        {
                                            LinkID = link.LinkID,
                                            LinkName = link.LinkName,
                                            LinkAddress = link.LinkAddress,
                                            LinkPicture = link.LinkPicture,
                                            LinkCategoryId = link.LinkCategoryId,
                                            LinkCategory = link.LinkCategory,
                                            CreatedAt = link.CreatedAt,
                                            LinkDescription = link.LinkDescription,
                                            UserAuthorId = link.UserAuthorId,
                                            UserAuthor = link.UserAuthor,
                                            PublicStatus = link.PublicStatus,
                                            FavoriteStatus = link.FavoriteStatus,
                                            LikeCount = link.LikeCount
                                        };

                        List<LinkVM> list = new List<LinkVM>();

                        foreach (var item in leadLinks)
                        {
                            list.Add(new LinkVM()
                            {
                                LinkID = item.LinkID,
                                LinkName = item.LinkName,
                                LinkAddress = item.LinkAddress,
                                LinkPicture = item.LinkPicture,
                                LinkCategoryId = item.LinkCategoryId,
                                LinkCategory = item.LinkCategory,
                                CreatedAt = item.CreatedAt,
                                LinkDescription = item.LinkDescription,
                                UserAuthorId = item.UserAuthorId,
                                UserAuthor = item.UserAuthor,
                                PublicStatus = item.PublicStatus,
                                FavoriteStatus = item.FavoriteStatus,
                                LikeCount = item.LikeCount
                            });
                        }

                        profileVM.MyLinks = modelVM.MyLinks;

                        if (filter == null)
                        {
                            profileVM.LinkListUserProfile = list.Where(x => x.PublicStatus == true).ToPagedList(pageNumber, 24);
                        }
                        else
                        {
                            profileVM.LinkListUserProfile = list.Where(x => x.PublicStatus == true && x.SearchLink().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 24);
                        }

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == userDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        ViewBag.Title = "Закладки избранных пользователей";

                        ViewBag.UserType = "Закладки избранных пользователей";

                        ViewBag.linkOnPage = profileVM.LinkListUserProfile;

                        return PartialView("_UsersPartial", profileVM);
                    }
                    catch (Exception)
                    {
                        return RedirectToAction("Error");
                    }
                }
                else//для дефолтной страницы Избранные пользователи
                {
                    try
                    {
                        var modelSubscriber = new ProfileVM()
                        {
                            SubscribeOnUserList = userDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == userDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList(),
                            MyLinks = userDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == userDTO.UsersProfileDTO.UserId).Select(x => new LinkVM(x)).ToList()
                        };

                        var usersLead = from leadUser in userDTO.UsersListDTO
                                        join subscriber in modelSubscriber.SubscribeOnUserList
                                        on leadUser.UserId equals subscriber.LeadUser
                                        select new
                                        {
                                            UserId = leadUser.UserId,
                                            Login = leadUser.Login,
                                            Email = leadUser.Email,
                                            Password = leadUser.Password,
                                            AvatarName = leadUser.AvatarName,
                                            LinksCount = leadUser.LinksCount,
                                            BanStatus = leadUser.BanStatus,
                                            Role = leadUser.Role
                                        };

                        List<UserVM> list = new List<UserVM>();

                        foreach (var item in usersLead)
                        {
                            list.Add(new UserVM()
                            {
                                UserId = item.UserId,
                                Login = item.Login,
                                Email = item.Email,
                                Password = item.Password,
                                AvatarName = item.AvatarName,
                                LinksCount = item.LinksCount,
                                BanStatus = item.BanStatus,
                                Role = item.Role
                            });
                        }

                        var userLeadLinkCount = from link in userDTO.LinkListUserProfileDTO
                                                join userLead in modelSubscriber.SubscribeOnUserList
                                                on link.UserAuthorId equals userLead.LeadUser
                                                select new
                                                {
                                                    LinkID = link.LinkID,
                                                    LinkName = link.LinkName,
                                                    LinkAddress = link.LinkAddress,
                                                    LinkPicture = link.LinkPicture,
                                                    LinkCategoryId = link.LinkCategoryId,
                                                    LinkCategory = link.LinkCategory,
                                                    CreatedAt = link.CreatedAt,
                                                    LinkDescription = link.LinkDescription,
                                                    UserAuthorId = link.UserAuthorId,
                                                    UserAuthor = link.UserAuthor,
                                                    PublicStatus = link.PublicStatus,
                                                    FavoriteStatus = link.FavoriteStatus,
                                                    LikeCount = link.LikeCount
                                                };

                        List<LinkVM> listLinkCount = new List<LinkVM>();

                        foreach (var item in userLeadLinkCount)
                        {
                            listLinkCount.Add(new LinkVM()
                            {
                                LinkID = item.LinkID,
                                LinkName = item.LinkName,
                                LinkAddress = item.LinkAddress,
                                LinkPicture = item.LinkPicture,
                                LinkCategoryId = item.LinkCategoryId,
                                LinkCategory = item.LinkCategory,
                                CreatedAt = item.CreatedAt,
                                LinkDescription = item.LinkDescription,
                                UserAuthorId = item.UserAuthorId,
                                UserAuthor = item.UserAuthor,
                                PublicStatus = item.PublicStatus,
                                FavoriteStatus = item.FavoriteStatus,
                                LikeCount = item.LikeCount
                            });
                        }

                        profileVM.AllUserLinks = listLinkCount.Where(x => x.PublicStatus == true).ToList();

                        profileVM.SubscribeOnUserList = modelSubscriber.SubscribeOnUserList;

                        profileVM.MyLinks = modelSubscriber.MyLinks;

                        ViewBag.Title = "Избранные пользователи";

                        ViewBag.UserType = "Избранные пользователи";

                        profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == userDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                        if (filter == null)
                        {
                            profileVM.UsersLeadList = list.ToPagedList(pageNumber, 30);

                            ViewBag.usersOnPage = profileVM.UsersLeadList;

                            return View(profileVM);
                        }
                        else
                        {
                            profileVM.UsersLeadList = list.Where(x => x.SearchInfo().ToLower().Contains(filter.ToLower())).ToPagedList(pageNumber, 30);

                            ViewBag.usersOnPage = profileVM.UsersLeadList;

                            return PartialView("_UsersPartial", profileVM);
                        }
                    }
                    catch (Exception)
                    {
                        return RedirectToAction("Error");
                    }
                }
            }
        }

        //сохранение закладок других пользователей
        public async Task<ActionResult> SaveOtherUserLink(int linkId, string title, int? page)
        {
            try
            {
                string login = User.Identity.Name;

                int pageNumber = page ?? 1;

                ProfileVM profileVM = new ProfileVM();

                using (var profile = new ChekitDB())
                {
                    var modelDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login),//юзер
                        UserLinkDTO = await profile.Links.FirstOrDefaultAsync(x => x.LinkID == linkId),//его закладки. После по этому списку текущий может видеть свои закладки - они будут
                        //выделены зеленым флоппи-диском
                        SubscribeOnUserListDTO = await profile.SubscribeOnUserDTO.ToListAsync()
                    };

                    LinkDTO savedLink = new LinkDTO()
                    {
                        LinkID = modelDTO.UserLinkDTO.LinkID,
                        CreatedAt = Convert.ToDateTime(DateTime.Now),
                        FavoriteStatus = false,
                        LikeCount = 0,
                        LinkAddress = modelDTO.UserLinkDTO.LinkAddress,
                        LinkCategory = "Сохраненные закладки",
                        LinkCategoryId = 0,
                        LinkDescription = modelDTO.UserLinkDTO.LinkDescription,
                        LinkName = modelDTO.UserLinkDTO.LinkName,
                        LinkPicture = modelDTO.UserLinkDTO.LinkPicture,
                        PublicStatus = false,
                        UserAuthor = modelDTO.UsersProfileDTO.Login,
                        UserAuthorId = modelDTO.UsersProfileDTO.UserId
                    };

                    profile.Links.Add(savedLink);

                    UsersDTO usersDTO = modelDTO.UsersProfileDTO;

                    usersDTO.LinksCount += 1;

                    await profile.SaveChangesAsync();

                    var linkDTO = new ProfileDTO()
                    {
                        LinkListUserProfileDTO = await profile.Links.ToListAsync()
                    };

                    var modelVM = new ProfileVM()
                    {
                        LinkListUserProfile = linkDTO.LinkListUserProfileDTO.Where(x => x.PublicStatus == true).ToArray().OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24),
                        SubscribeOnUserList = modelDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == modelDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList(),
                        MyLinks = linkDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == modelDTO.UsersProfileDTO.UserId).Select(x => new LinkVM(x)).ToList()
                    };

                    var leadLinks = from link in linkDTO.LinkListUserProfileDTO
                                    join leadUser in modelVM.SubscribeOnUserList
                                    on link.UserAuthorId equals leadUser.LeadUser
                                    select new
                                    {
                                        LinkID = link.LinkID,
                                        LinkName = link.LinkName,
                                        LinkAddress = link.LinkAddress,
                                        LinkPicture = link.LinkPicture,
                                        LinkCategoryId = link.LinkCategoryId,
                                        LinkCategory = link.LinkCategory,
                                        CreatedAt = link.CreatedAt,
                                        LinkDescription = link.LinkDescription,
                                        UserAuthorId = link.UserAuthorId,
                                        UserAuthor = link.UserAuthor,
                                        PublicStatus = link.PublicStatus,
                                        FavoriteStatus = link.FavoriteStatus,
                                        LikeCount = link.LikeCount
                                    };

                    List<LinkVM> list = new List<LinkVM>();

                    foreach (var item in leadLinks)
                    {
                        list.Add(new LinkVM()
                        {
                            LinkID = item.LinkID,
                            LinkName = item.LinkName,
                            LinkAddress = item.LinkAddress,
                            LinkPicture = item.LinkPicture,
                            LinkCategoryId = item.LinkCategoryId,
                            LinkCategory = item.LinkCategory,
                            CreatedAt = item.CreatedAt,
                            LinkDescription = item.LinkDescription,
                            UserAuthorId = item.UserAuthorId,
                            UserAuthor = item.UserAuthor,
                            PublicStatus = item.PublicStatus,
                            FavoriteStatus = item.FavoriteStatus,
                            LikeCount = item.LikeCount
                        });
                    }

                    profileVM.MyLinks = modelVM.MyLinks;

                    if (title == "Общая лента публичных закладок")
                    {
                        profileVM.LinkListUserProfile = linkDTO.LinkListUserProfileDTO.Where(x => x.LinkCategory != "Сохраненные закладки" && x.PublicStatus == true).OrderByDescending(x => x.CreatedAt).Select(x => new LinkVM(x)).ToPagedList(pageNumber, 24);
                    }
                    else
                    {
                        profileVM.LinkListUserProfile = list.Where(x => x.PublicStatus == true && x.LinkCategory != "Сохраненные закладки").ToPagedList(pageNumber, 24);
                    }

                    profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == modelDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                    ViewBag.Title = title;

                    ViewBag.linkOnPage = profileVM.LinkListUserProfile;

                    TempData["OK"] = "Закладка сохранена";

                    if (title == "Общая лента публичных закладок")
                    {
                        return PartialView("_UserProfilePartial", profileVM);
                    }
                    else
                    {
                        return PartialView("_UsersPartial", profileVM);
                    }
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //подписка на другого пользователя
        public async Task<ActionResult> Subscribe(string status, int userId, string title, int? page)
        {
            try
            {
                ProfileVM profileVM = new ProfileVM();

                int pageNumber = page ?? 1;

                string login = User.Identity.Name;

                using (var profile = new ChekitDB())
                {
                    var userDTO = new ProfileDTO()
                    {
                        UsersProfileDTO = await profile.Users.FirstOrDefaultAsync(x => x.Login == login)
                    };

                    if (status == "Подписаться")
                    {
                        SubscribeOnUserDTO subscribeOnUser = new SubscribeOnUserDTO()
                        {
                            UserSubscriber = userDTO.UsersProfileDTO.UserId,
                            SubscribeStatus = true,
                            LeadUser = userId
                        };

                        TempData["OK"] = "Подписка оформлена!";

                        profile.SubscribeOnUserDTO.Add(subscribeOnUser);

                        await profile.SaveChangesAsync();
                    }

                    if (status == "Отписаться")
                    {
                        SubscribeOnUserDTO subscribeOnUser = await profile.SubscribeOnUserDTO.FirstOrDefaultAsync(x => x.LeadUser == userId);

                        TempData["OK"] = "Подписка отозвана!";

                        profile.SubscribeOnUserDTO.Remove(subscribeOnUser);

                        await profile.SaveChangesAsync();
                    }

                    var modelDTO = new ProfileDTO()
                    {
                        LinkListUserProfileDTO = await profile.Links.ToListAsync(),
                        SubscribeOnUserListDTO = await profile.SubscribeOnUserDTO.ToListAsync(),
                        UsersListDTO = await profile.Users.ToListAsync()
                    };

                    var modelVM = new ProfileVM()
                    {
                        SubscribeOnUserList = modelDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == userDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList(),
                        MyLinks = modelDTO.LinkListUserProfileDTO.Where(x => x.UserAuthorId == userDTO.UsersProfileDTO.UserId).Select(x => new LinkVM(x)).ToList()
                    };

                    if (title == "Подписаны на меня")
                    {
                        var modelLeadVM = new ProfileVM()
                        {
                            SubscribeOnUserList = modelDTO.SubscribeOnUserListDTO.Where(x => x.LeadUser == userDTO.UsersProfileDTO.UserId && x.SubscribeStatus == true).Select(x => new SubscribeOnUserVM(x)).ToList()
                        };

                        var modelSubscriber = new ProfileVM()
                        {
                            SubscribeOnUserList = modelDTO.SubscribeOnUserListDTO.Where(x => x.UserSubscriber == userDTO.UsersProfileDTO.UserId || x.LeadUser == userDTO.UsersProfileDTO.UserId).Select(x => new SubscribeOnUserVM(x)).ToList()
                        };

                        var usersLead = from leadUser in modelDTO.UsersListDTO
                                        join subscriber in modelLeadVM.SubscribeOnUserList
                                        on leadUser.UserId equals subscriber.UserSubscriber
                                        select new
                                        {
                                            UserId = leadUser.UserId,
                                            Login = leadUser.Login,
                                            Email = leadUser.Email,
                                            Password = leadUser.Password,
                                            AvatarName = leadUser.AvatarName,
                                            LinksCount = leadUser.LinksCount,
                                            BanStatus = leadUser.BanStatus,
                                            Role = leadUser.Role
                                        };

                        List<UserVM> subsribers = new List<UserVM>();

                        foreach (var item in usersLead)
                        {
                            subsribers.Add(new UserVM()
                            {
                                UserId = item.UserId,
                                Login = item.Login,
                                Email = item.Email,
                                Password = item.Password,
                                AvatarName = item.AvatarName,
                                LinksCount = item.LinksCount,
                                BanStatus = item.BanStatus,
                                Role = item.Role
                            });
                        }

                        profileVM.UsersLeadList = subsribers.ToPagedList(pageNumber, 30);

                        profileVM.SubscribeOnUserList = modelSubscriber.SubscribeOnUserList;
                    }
                    else if (title == "Избранные пользователи")
                    {
                        var usersLead = from leadUser in modelDTO.UsersListDTO
                                        join subscriber in modelVM.SubscribeOnUserList
                                        on leadUser.UserId equals subscriber.LeadUser
                                        where subscriber.SubscribeStatus == true
                                        select new
                                        {
                                            UserId = leadUser.UserId,
                                            Login = leadUser.Login,
                                            Email = leadUser.Email,
                                            Password = leadUser.Password,
                                            AvatarName = leadUser.AvatarName,
                                            LinksCount = leadUser.LinksCount,
                                            BanStatus = leadUser.BanStatus,
                                            Role = leadUser.Role
                                        };

                        List<UserVM> choisenUsers = new List<UserVM>();

                        foreach (var item in usersLead)
                        {
                            choisenUsers.Add(new UserVM()
                            {
                                UserId = item.UserId,
                                Login = item.Login,
                                Email = item.Email,
                                Password = item.Password,
                                AvatarName = item.AvatarName,
                                LinksCount = item.LinksCount,
                                BanStatus = item.BanStatus,
                                Role = item.Role
                            });
                        }

                        profileVM.UsersLeadList = choisenUsers.ToPagedList(pageNumber, 30);

                        profileVM.SubscribeOnUserList = modelVM.SubscribeOnUserList;
                    }
                    else
                    {
                        var allUsersVM = new ProfileVM()
                        {
                            UsersLeadList = modelDTO.UsersListDTO.Where(x => x.BanStatus == false && x.UserId != userDTO.UsersProfileDTO.UserId).Select(x => new UserVM(x)).ToPagedList(pageNumber, 30)
                        };

                        profileVM.UsersLeadList = allUsersVM.UsersLeadList;

                        profileVM.SubscribeOnUserList = modelVM.SubscribeOnUserList;
                    }

                    var leadLinks = from link in modelDTO.LinkListUserProfileDTO
                                    join leadUser in modelVM.SubscribeOnUserList
                                    on link.UserAuthorId equals leadUser.LeadUser
                                    select new
                                    {
                                        LinkID = link.LinkID,
                                        LinkName = link.LinkName,
                                        LinkAddress = link.LinkAddress,
                                        LinkPicture = link.LinkPicture,
                                        LinkCategoryId = link.LinkCategoryId,
                                        LinkCategory = link.LinkCategory,
                                        CreatedAt = link.CreatedAt,
                                        LinkDescription = link.LinkDescription,
                                        UserAuthorId = link.UserAuthorId,
                                        UserAuthor = link.UserAuthor,
                                        PublicStatus = link.PublicStatus,
                                        FavoriteStatus = link.FavoriteStatus,
                                        LikeCount = link.LikeCount
                                    };

                    List<LinkVM> list = new List<LinkVM>();

                    foreach (var item in leadLinks)
                    {
                        list.Add(new LinkVM()
                        {
                            LinkID = item.LinkID,
                            LinkName = item.LinkName,
                            LinkAddress = item.LinkAddress,
                            LinkPicture = item.LinkPicture,
                            LinkCategoryId = item.LinkCategoryId,
                            LinkCategory = item.LinkCategory,
                            CreatedAt = item.CreatedAt,
                            LinkDescription = item.LinkDescription,
                            UserAuthorId = item.UserAuthorId,
                            UserAuthor = item.UserAuthor,
                            PublicStatus = item.PublicStatus,
                            FavoriteStatus = item.FavoriteStatus,
                            LikeCount = item.LikeCount
                        });
                    }

                    profileVM.AllUserLinks = list.Where(x => x.PublicStatus == true).ToList();

                    profileVM.MyLinks = modelVM.MyLinks;

                    profileVM.LinkListUserProfile = list.Where(x => x.PublicStatus == true).ToPagedList(pageNumber, 24);

                    profileVM.CategoryList = new SelectList(await profile.Categories.Where(x => x.UserCategory == userDTO.UsersProfileDTO.UserId || x.CategoryName == "Все ссылки").ToListAsync(), "CategoryId", "CategoryName");

                    ViewBag.linkOnPage = profileVM.LinkListUserProfile;

                    return PartialView("_UsersPartial", profileVM);
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Error");
            }
        }

        //защита
        public ActionResult Error()
        {
            return View("ExitError");
        }
    }
}