namespace CodeAnalysis;
internal static class MethodHeaderAnalyzer
{
    public static (bool isMethod, int position) IsMethod(string line, string? className)
    {
        CapitalData capitalData = new();
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            capitalData.Update(i, ch);
            if (ch == '=' || ch == '[') break; // against annotations
            if (ch == '(' && capitalData.CapitalUsed)
            {
                string lineSoFar = line[0..i];
                return lineSoFar == className || lineSoFar.Contains(' ') ? (true, i) : (false, 0);
            }
        }
        return (false, 0);
    }
}

internal sealed class CapitalData
{
    public bool CapitalUsed { get; set; }
    public int LastCapitalIndex { get; set; }

    public void Update(int i, char ch)
    {
        if (ch == ' ') CapitalUsed = false;
        if (char.IsUpper(ch) && !CapitalUsed)
        {
            CapitalUsed = true;
            LastCapitalIndex = i;
        }
    }
}

