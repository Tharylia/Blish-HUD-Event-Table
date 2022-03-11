namespace Estreya.BlishHUD.EventTable.Extensions
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public static class ObjectExtensions
    {
        public static T DeepCopy<T>(this T item)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, item);
            stream.Seek(0, SeekOrigin.Begin);
            T result = (T)formatter.Deserialize(stream);
            stream.Close();
            return result;
        }
    }
}
