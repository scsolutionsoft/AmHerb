using System.Text;

namespace AmHerb.Web.Services;
public static class AdminConsole
{
    public static void ReadCredentials()
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("Run --setup-admin in an interactive terminal, or supply AMHERB_ADMIN_EMAIL and AMHERB_ADMIN_PASSWORD with --migrate.");
        Console.Write("New SuperAdmin email: "); var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out _)) throw new InvalidOperationException("A valid new administrator email is required.");
        Console.Write("Password (12+ characters, upper/lower/number/symbol): ");
        var password = ReadPassword(); Console.Write("Confirm password: "); var confirmation = ReadPassword();
        if (password != confirmation) throw new InvalidOperationException("Passwords do not match.");
        Environment.SetEnvironmentVariable("AMHERB_ADMIN_EMAIL", email);
        Environment.SetEnvironmentVariable("AMHERB_ADMIN_PASSWORD", password);
    }
    private static string ReadPassword()
    {
        var value = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
            if (key.Key == ConsoleKey.Backspace) { if (value.Length > 0) value.Length--; continue; }
            if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
        }
    }
}
