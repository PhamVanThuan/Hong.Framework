
namespace Hong.Common.Extendsion
{
    public static class Object
    {
        /// <summary>类型转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from">来源</param>
        /// <param name="deafult">转换失败时的默认值</param>
        /// <returns></returns>
        public static T TryToType<T>(this object from, T deafult)
        {
            try
            {
                return (T)System.Convert.ChangeType(from, typeof(T));
            }
            catch
            {
                return deafult;
            }
        }
    }
}
