using System.Text;

namespace Hrevolve.Agent.Services.Logging;

public static class LogSanitizer
{
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                _ when char.IsControl(ch) => $"\\u{(int)ch:x4}",
                _ => ch.ToString()
            });
        }

        return sb.ToString();
    }
}

