using System.ComponentModel.DataAnnotations;
using System.Text;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public static class AdminSetup
{
    public static async Task RunAsync(UserService users)
    {
        Console.Write("Admin email: ");
        var email = Console.ReadLine() ?? string.Empty;
        Console.Write("Password (6-100 characters): ");
        string password;
        if (Console.IsInputRedirected) password = Console.ReadLine() ?? string.Empty;
        else
        {
            var buffer = new StringBuilder();
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace && buffer.Length > 0) buffer.Length--;
                else if (!char.IsControl(key.KeyChar)) buffer.Append(key.KeyChar);
            }
            Console.WriteLine();
            password = buffer.ToString();
        }
        var data = new RegisterViewModel
        {
            Email = email.Trim(), Password = password, ConfirmPassword = password,
            FirstName = "ผู้ดูแล", LastName = "ระบบ"
        };
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(data, new ValidationContext(data), errors, true))
        {
            foreach (var error in errors) Console.WriteLine(error.ErrorMessage);
            Environment.ExitCode = 1;
            return;
        }
        var result = await users.CreateAsync(data, "Admin");
        Console.WriteLine(result.DuplicateEmail ? "อีเมลนี้มีบัญชีอยู่แล้ว กรุณาใช้อีเมลอื่น" : "สร้างผู้ดูแลสำเร็จ ใช้อีเมลนี้เข้าสู่ระบบได้แล้ว");
        if (result.DuplicateEmail) Environment.ExitCode = 1;
    }
}
