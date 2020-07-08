public static class Format
{
    private const int TabLength = 4;

    public static string NewLine(int tabs = 0)
    {
        return System.Environment.NewLine + Spaces(TabLength * tabs);
    }

    public static string Space
    {
        get
        {
            return " ";
        }
    }

    public static string Spaces(int count)
    {
        string result = string.Empty;
        for (int i = 0; i < count; i++)
        {
            result += Space;
        }
        return result;
    }
}