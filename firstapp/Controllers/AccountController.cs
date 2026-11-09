using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PrescriptionAPI;

public class AccountController : Controller
{
    private readonly PrescriptionDAL _dal;

    public AccountController(IConfiguration configuration)
    {
        _dal = new PrescriptionDAL(configuration);
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Signup()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required";
            return View();
        }

        var result = await _dal.ValidateUserAsync(email, password);

        if (result.Item1 != null)
        {
            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetInt32("UserId", result.Item1.Value);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("FullName", result.Item2);

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Invalid email or password";
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Signup(string fullName, string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required";
            return View();
        }

        string result = await _dal.RegisterUserAsync(fullName, email, password);

        if (result == "EXISTS")
        {
            ViewBag.Error = "Email already registered";
            return View();
        }

        HttpContext.Session.SetString("IsLoggedIn", "true");
        HttpContext.Session.SetString("FullName", fullName);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account");
    }
}
