namespace Notcomd.DomainCommand;

public static class FormattableStringHelper
{
    public static string UriBuilder(FormattableString formattableString)
    {
        var initail = formattableString.GetArguments()
            .Select(en => FormattableString.Invariant($"{en}"));
        var pro = initail.Select(en => (object)Uri.EscapeDataString(en)).ToArray();
        return string.Format(formattableString.Format, pro);
    }
}