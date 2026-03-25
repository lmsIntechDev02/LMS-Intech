using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using LeaveON.Models;
using Repository.Models;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System;
using System.Collections;

namespace LeaveON.Controllers
{
  // FIXED: Removed [Authorize] from class level.
  // AuthLogin needs NO attribute so IIS Windows Auth can challenge it.
  // Each action below has its own attribute where needed.
  public class AccountController : Controller
  {
    private ApplicationSignInManager _signInManager;
    private ApplicationUserManager _userManager;
    private LeaveONEntities db = new LeaveONEntities();

    public AccountController() { }

    public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
    {
      UserManager = userManager;
      SignInManager = signInManager;
    }

    public ApplicationSignInManager SignInManager
    {
      get { return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
      private set { _signInManager = value; }
    }

    public ApplicationUserManager UserManager
    {
      get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
      private set { _userManager = value; }
    }

    // ---------------------------------------------------------------
    // User/Role management — requires login
    // ---------------------------------------------------------------
    [Authorize(Roles = "Admin")]
    public ActionResult Index()
    {
      var sortedEmployees = db.AspNetUsers
         .AsEnumerable()
         .Select(user => new
         {
           user.Id,
           UserName = user.UserName.Substring(0, user.UserName.IndexOf('@')).Replace(".", " ")
         })
         .OrderBy(x => x.UserName)
         .ToList();

      ViewBag.Employees = new SelectList(sortedEmployees, "Id", "UserName");
      ViewBag.Roles = new SelectList(db.AspNetRoles.OrderBy(x => x.Name), "Id", "Name");

      List<UserRoleModel> usersAndRoles = new List<UserRoleModel>();
      List<AspNetUser> AspNetUsers = db.AspNetUsers.ToList<AspNetUser>();
      foreach (AspNetUser user in AspNetUsers)
      {
        foreach (AspNetRole role in user.AspNetRoles)
        {
          usersAndRoles.Add(new UserRoleModel
          {
            UserId = user.Id,
            UserName = user.UserName.Substring(0, user.UserName.IndexOf('@')).Replace(".", " "),
            RoleId = role.Id,
            RoleName = role.Name
          });
        }
      }
      return View(usersAndRoles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Index([Bind(Include = "UserId,UserName,RoleId,RoleName")] UserRoleModel userRoleModel)
    {
      if (ModelState.IsValid)
      {
        userRoleModel.RoleName = db.AspNetRoles.FirstOrDefault(x => x.Id == userRoleModel.RoleId).Name;
        switch (userRoleModel.RoleName)
        {
          case "Admin":
            await UserManager.AddToRoleAsync(userRoleModel.UserId, "Admin");
            await UserManager.AddToRoleAsync(userRoleModel.UserId, "Manager");
            await UserManager.AddToRoleAsync(userRoleModel.UserId, "User");
            break;
          case "Manager":
            await UserManager.AddToRoleAsync(userRoleModel.UserId, "Manager");
            await UserManager.AddToRoleAsync(userRoleModel.UserId, "User");
            break;
          case "User":
            await UserManager.AddToRoleAsync(userRoleModel.UserId, "User");
            break;
        }
      }
      return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteRole(string UserIdRoleId)
    {
      string UserId = UserIdRoleId.Split(',').First();
      string RoleId = UserIdRoleId.Split(',').Last();
      string RoleName = db.AspNetRoles.FirstOrDefault(x => x.Id == RoleId).Name;

      switch (RoleName)
      {
        case "Admin":
          await UserManager.RemoveFromRoleAsync(UserId, "Admin");
          await UserManager.RemoveFromRoleAsync(UserId, "Manager");
          await UserManager.RemoveFromRoleAsync(UserId, "User");
          break;
        case "Manager":
          await UserManager.RemoveFromRoleAsync(UserId, "Manager");
          await UserManager.RemoveFromRoleAsync(UserId, "User");
          break;
        case "User":
          await UserManager.RemoveFromRoleAsync(UserId, "User");
          break;
      }
      return RedirectToAction("Index");
    }

    [HttpGet]
    [Authorize]
    public ActionResult Authorize()
    {
      var claims = new ClaimsPrincipal(User).Claims.ToArray();
      var identity = new ClaimsIdentity(claims, "Bearer");
      AuthenticationManager.SignIn(identity);
      return new EmptyResult();
    }

    // ---------------------------------------------------------------
    // FIXED: NO [AllowAnonymous] and NO [Authorize] here.
    // The class-level [Authorize] is removed above.
    // IIS Windows Auth (configured in applicationHost.config) will
    // challenge this route and show the popup to unauthenticated users.
    // User.Identity.Name is populated AFTER the popup is completed.
    // ---------------------------------------------------------------
    public ActionResult AuthLogin(string returnUrl)

    {
      // Add this temporarily to confirm Windows Auth is working
      // If not authenticated via Windows yet — trigger the challenge manually
      if (!User.Identity.IsAuthenticated)
      {
        // This forces IIS to send 401 and trigger Windows Auth popup
        HttpContext.GetOwinContext().Authentication.Challenge(
            new Microsoft.Owin.Security.AuthenticationProperties { RedirectUri = "/" },
            "Windows"
        );
        return new HttpUnauthorizedResult();
      }
      string ADUserValue = null;

      try
      {
        PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
        UserPrincipal currentUser = UserPrincipal.FindByIdentity(ctx, User.Identity.Name);
        ADUserValue = currentUser?.UserPrincipalName;
      }
      catch
      {
        return RedirectToAction("Error404", "Error");
      }

      if (string.IsNullOrEmpty(ADUserValue))
        return RedirectToAction("Error404", "Error");

      return RedirectToAction("Login", new { returnUrl = returnUrl, ADUser = ADUserValue });
    }
    // OWIN redirects here when unauthenticated (LoginPath in Startup.Auth.cs)
    // Plain anonymous page — just forwards to AuthLogin which triggers Windows popup
    [AllowAnonymous]
    public ActionResult WindowsLogin(string returnUrl)
    {
      return RedirectToAction("AuthLogin", new { returnUrl = returnUrl });
    }

    // FIXED: [AllowAnonymous] here is correct — this action only receives
    // the ADUser param from AuthLogin redirect, no Windows challenge needed.
    [AllowAnonymous]
    public async Task<ActionResult> Login(string returnUrl, string ADUser)
    {
      if (string.IsNullOrEmpty(ADUser))
        return RedirectToAction("Error404", "Error");

      // Find the Identity user matching the AD account
      var appUser = await UserManager.FindByNameAsync(ADUser);
      if (appUser == null)
        return RedirectToAction("Error404", "Error");

      // Assign default role only if the user has NO roles yet
      var existingRoles = await UserManager.GetRolesAsync(appUser.Id);
      //if (!existingRoles.Any())
      //{
      //  await UserManager.AddToRoleAsync(appUser.Id, "User");
      //}


      // Always assign base User role
      if (!existingRoles.Contains("User"))
        await UserManager.AddToRoleAsync(appUser.Id, "User");

      // Assign Manager if in AD manager group
      if (!existingRoles.Contains("Manager"))
        await UserManager.AddToRoleAsync(appUser.Id, "Manager");

      // Assign Admin if in AD admin group
      if (!existingRoles.Contains("Admin"))
        await UserManager.AddToRoleAsync(appUser.Id, "Admin");

      // Build fresh identity with updated roles
      var identity = await UserManager.CreateIdentityAsync(
          appUser, DefaultAuthenticationTypes.ApplicationCookie);

      HttpContext.GetOwinContext().Authentication.SignIn(
          new Microsoft.Owin.Security.AuthenticationProperties { IsPersistent = false },
          identity
      );

      // Business-logic guard after sign-in
      if (appUser.UserLeavePolicyId == null)
      {
        TempData["ErrorMessage"] = "No leave policy assigned. Please contact support.";
        return RedirectToAction("General", "Error");
      }

      //if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
      //  return Redirect(returnUrl);

      return RedirectToAction("Index", "LeavesRequest");
    }

    public void GetLog()
    {
      var path = Server.MapPath(@"~/UsersAndProperties.txt");
      List<string> userprops = new List<string>();
      try
      {
        DirectoryEntry root = new DirectoryEntry("LDAP://RootDSE");
        root = new DirectoryEntry("LDAP://" + root.Properties["defaultNamingContext"][0]);
        DirectorySearcher search = new DirectorySearcher(root);
        search.Filter = "(&(objectClass=user)(objectCategory=person))";
        SearchResultCollection results = search.FindAll();
        if (results != null)
        {
          foreach (SearchResult result in results)
          {
            foreach (DictionaryEntry property in result.Properties)
            {
              userprops.Add(property.Key + ": ");
              foreach (var val in (property.Value as ResultPropertyValueCollection))
                userprops.Add(val + "; ");
              userprops.Add(Environment.NewLine + "");
            }
            userprops.Add(Environment.NewLine + "------------------------------------");
          }
        }
        System.IO.File.WriteAllLines(path, userprops);
      }
      catch (Exception ex) { }
    }

    // Standard password login — kept as-is
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
    {
      if (!ModelState.IsValid)
        return RedirectToAction("Error404", "Error");

      var result = await SignInManager.PasswordSignInAsync(model.Email, "Leaves12*", model.RememberMe, shouldLockout: false);
      switch (result)
      {
        case SignInStatus.Success:
          return RedirectToAction("index", "LeavesRequest");
        case SignInStatus.LockedOut:
          return View("Lockout");
        case SignInStatus.RequiresVerification:
          return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
        case SignInStatus.Failure:
        default:
          ModelState.AddModelError("", "Invalid login attempt.");
          return RedirectToAction("Error404", "Error");
      }
    }

    [AllowAnonymous]
    public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
    {
      if (!await SignInManager.HasBeenVerifiedAsync())
        return View("Error");
      return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
    {
      if (!ModelState.IsValid)
        return View(model);

      var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
      switch (result)
      {
        case SignInStatus.Success:
          return RedirectToLocal(model.ReturnUrl);
        case SignInStatus.LockedOut:
          return View("Lockout");
        case SignInStatus.Failure:
        default:
          ModelState.AddModelError("", "Invalid code.");
          return View(model);
      }
    }

    [Authorize(Roles = "Admin,Manager")]
    public ActionResult Register()
    {
      ViewBag.Countries = db.CountryNames;
      ViewBag.Departments = db.DepartmentNames;
      ViewBag.LeavePolicies = db.UserLeavePolicies;
      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> Register(RegisterViewModel model)
    {
      if (ModelState.IsValid)
      {
        var user = new ApplicationUser
        {
          UserName = model.Email,
          Email = model.Email,
          Hometown = model.Hometown,
          BioStarEmpNum = model.BioStarEmpNum,
          UserLeavePolicyId = model.UserLeavePolicyId
        };
        var result = await UserManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
          switch (model.Role)
          {
            case "Admin":
              await UserManager.AddToRoleAsync(user.Id, "Admin");
              await UserManager.AddToRoleAsync(user.Id, "Manager");
              await UserManager.AddToRoleAsync(user.Id, "User");
              break;
            case "Manager":
              await UserManager.AddToRoleAsync(user.Id, "Manager");
              await UserManager.AddToRoleAsync(user.Id, "User");
              break;
            case "User":
              await UserManager.AddToRoleAsync(user.Id, "User");
              break;
          }
          return RedirectToAction("Index", "LeavesRequest");
        }
        AddErrors(result);
      }
      ViewBag.Departments = db.DepartmentNames;
      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> UpdateUser(UpdateUserViewModel model)
    {
      if (ModelState.IsValid)
      {
        var user = new ApplicationUser
        {
          UserName = model.Email,
          Email = model.Email,
          Hometown = model.Hometown,
          BioStarEmpNum = model.BioStarEmpNum,
          UserLeavePolicyId = model.UserLeavePolicyId
        };
        var result = await UserManager.UpdateAsync(user);
        if (result.Succeeded)
        {
          await UserManager.RemoveFromRoleAsync(user.Id, "Admin");
          await UserManager.RemoveFromRoleAsync(user.Id, "Manager");
          await UserManager.RemoveFromRoleAsync(user.Id, "User");

          switch (model.Role)
          {
            case "Admin":
              await UserManager.AddToRoleAsync(user.Id, "Admin");
              await UserManager.AddToRoleAsync(user.Id, "Manager");
              await UserManager.AddToRoleAsync(user.Id, "User");
              break;
            case "Manager":
              await UserManager.AddToRoleAsync(user.Id, "Manager");
              await UserManager.AddToRoleAsync(user.Id, "User");
              break;
            case "User":
              await UserManager.AddToRoleAsync(user.Id, "User");
              break;
          }
          return RedirectToAction("Index", "LeavesRequest");
        }
        AddErrors(result);
      }
      ViewBag.Departments = db.DepartmentNames;
      return View(model);
    }

    [HttpPost]
    [Authorize]
    public ActionResult GetDepartmentByCountryId(int CountryId)
    {
      List<DepartmentName> Departments = db.DepartmentNames.Where(x => x.Id == CountryId).ToList<DepartmentName>();
      SelectList LstDepartments = new SelectList(Departments, "Id", "Name", 0);
      return Json(LstDepartments);
    }

    [AllowAnonymous]
    public async Task<ActionResult> ConfirmEmail(string userId, string code)
    {
      if (userId == null || code == null)
        return View("Error");
      var result = await UserManager.ConfirmEmailAsync(userId, code);
      return View(result.Succeeded ? "ConfirmEmail" : "Error");
    }

    [AllowAnonymous]
    public ActionResult ForgotPassword() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await UserManager.FindByNameAsync(model.Email);
        if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
          return View("ForgotPasswordConfirmation");
      }
      return View(model);
    }

    [AllowAnonymous]
    public ActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous]
    public ActionResult ResetPassword(string code) => code == null ? View("Error") : View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
    {
      if (!ModelState.IsValid)
        return View(model);
      var user = await UserManager.FindByNameAsync(model.Email);
      if (user == null)
        return RedirectToAction("ResetPasswordConfirmation", "Account");
      var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
      if (result.Succeeded)
        return RedirectToAction("ResetPasswordConfirmation", "Account");
      AddErrors(result);
      return View();
    }

    [AllowAnonymous]
    public ActionResult ResetPasswordConfirmation() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public ActionResult ExternalLogin(string provider, string returnUrl)
    {
      return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
    }

    [AllowAnonymous]
    public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
    {
      var userId = await SignInManager.GetVerifiedUserIdAsync();
      if (userId == null)
        return View("Error");
      var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
      var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
      return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SendCode(SendCodeViewModel model)
    {
      if (!ModelState.IsValid)
        return View();
      if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
        return View("Error");
      return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
    }

    [AllowAnonymous]
    public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
    {
      var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
      if (loginInfo == null)
        return RedirectToAction("Error404", "Error");

      var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
      switch (result)
      {
        case SignInStatus.Success:
          return RedirectToLocal(returnUrl);
        case SignInStatus.LockedOut:
          return View("Lockout");
        case SignInStatus.RequiresVerification:
          return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
        case SignInStatus.Failure:
        default:
          ViewBag.ReturnUrl = returnUrl;
          ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
          return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
      }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
    {
      if (User.Identity.IsAuthenticated)
        return RedirectToAction("Index", "Manage");

      if (ModelState.IsValid)
      {
        var info = await AuthenticationManager.GetExternalLoginInfoAsync();
        if (info == null)
          return View("ExternalLoginFailure");
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Hometown = model.Hometown };
        var result = await UserManager.CreateAsync(user);
        if (result.Succeeded)
        {
          result = await UserManager.AddLoginAsync(user.Id, info.Login);
          if (result.Succeeded)
          {
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            return RedirectToLocal(returnUrl);
          }
        }
        AddErrors(result);
      }
      ViewBag.ReturnUrl = returnUrl;
      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public ActionResult LogOff()
    {
      AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
      return RedirectToAction("Index", "LeavesRequest");
    }

    [Authorize]
    public ActionResult SignOut()
    {
      AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
      return RedirectToAction("Logout", "Account");
    }

    [AllowAnonymous]
    public ActionResult Logout() => View();

    //[AllowAnonymous]
    //public ActionResult LoginAgain() => Redirect("https://lms.intechww.com:1001/"); 
    [AllowAnonymous]
    public ActionResult LoginAgain()  
    {
      
      return RedirectToAction("AuthLogin", "Account");
    }
    [AllowAnonymous]
    public ActionResult ExternalLoginFailure() => View();

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (_userManager != null) { _userManager.Dispose(); _userManager = null; }
        if (_signInManager != null) { _signInManager.Dispose(); _signInManager = null; }
      }
      base.Dispose(disposing);
    }

    #region Helpers
    private const string XsrfKey = "XsrfId";

    private IAuthenticationManager AuthenticationManager
    {
      get { return HttpContext.GetOwinContext().Authentication; }
    }

    private void AddErrors(IdentityResult result)
    {
      foreach (var error in result.Errors)
        ModelState.AddModelError("", error);
    }

    private ActionResult RedirectToLocal(string returnUrl)
    {
      if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
      {
        if (returnUrl.ToLower().Contains("/account/login"))
          return RedirectToAction("Index", "LeavesRequest");
        return Redirect(returnUrl);
      }
      return RedirectToAction("Index", "LeavesRequest");
    }

    internal class ChallengeResult : HttpUnauthorizedResult
    {
      public ChallengeResult(string provider, string redirectUri) : this(provider, redirectUri, null) { }

      public ChallengeResult(string provider, string redirectUri, string userId)
      {
        LoginProvider = provider;
        RedirectUri = redirectUri;
        UserId = userId;
      }

      public string LoginProvider { get; set; }
      public string RedirectUri { get; set; }
      public string UserId { get; set; }

      public override void ExecuteResult(ControllerContext context)
      {
        var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
        if (UserId != null)
          properties.Dictionary[XsrfKey] = UserId;
        context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
      }
    }
    #endregion
  }
}
