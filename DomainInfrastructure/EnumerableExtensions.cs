namespace Notcomd.DomainCommand;

public static class EnumerableExtensions
{
    public static bool SequenceIgnoredEqual<Ty>(this IEnumerable<Ty> enumerable, IEnumerable<Ty> enumerable2)
    {
        if (enumerable == enumerable2)
        {
        }
        else if (enumerable == null || enumerable2 == null)
        {
            return false;
        }

        return enumerable.OrderBy(e => e).SequenceEqual(enumerable2.OrderBy(e => e));
    }
}