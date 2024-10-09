
namespace QueryMapper
{
    internal class TypeHelper
    {

        /// <summary>
        /// Checks whether type is a custom class other than system defined
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsActualClass(Type type)
        {
            var checkedtype = Nullable.GetUnderlyingType(type) ?? type;
            return checkedtype.IsClass && !checkedtype.IsEnum && checkedtype != typeof(string);
        }
    }
}
