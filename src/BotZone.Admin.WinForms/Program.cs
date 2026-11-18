namespace BotZone.Admin.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) =>
        {
            if(args.Contains("--smoke"))
            {
                var at=Array.IndexOf(args,"--output");var folder=at>=0&&at+1<args.Length?args[at+1]:Path.Combine("artifacts","winforms");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"fatal-ui-error.txt"),e.Exception.ToString());File.WriteAllText(Path.Combine(folder,"smoke.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=false,fatalUiError=e.Exception.Message,finished=DateTimeOffset.UtcNow}));Environment.Exit(2);
            }
            else MessageBox.Show(e.Exception.Message, "BotZone", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        if (args.Contains("--smoke")) Application.Run(new SmokeForm(args));
        else Application.Run(new LoginForm());
    }
}
