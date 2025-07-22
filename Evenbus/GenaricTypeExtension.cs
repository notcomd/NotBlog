namespace Notcomd.Evenbus
{
    public static class GenaricTypeExtension
    {

        public static string GetGenericTypeName(this Type type)
        {
            string typeName = string.Empty;

            if (type.IsGenericType)
            {
                var genericArguments = string.Join(", ", type.GetGenericArguments().Select(x => x.Name).ToArray());
                typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericArguments}>";
            }
            else
            {
                typeName = type.Name;
            }
            return typeName;
        }


        public static string GetGenericTypeName(this object @object)
        {
            return @object.GetType().GetGenericTypeName();
        }

    }
}
