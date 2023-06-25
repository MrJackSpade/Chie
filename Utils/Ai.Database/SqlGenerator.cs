using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace Loxifi.Database
{
    public class SqlGenerator
    {
        public string? FormatArgument(object? o)
        {
            if (o is null)
            {
                return "null";
            }

            if (o is string s)
            {
                return $"N'{s.Replace("'", "''")}'";
            }

            if (o is DateTime dt)
            {
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
            }

            if (o is bool b)
            {
                return b ? "1" : "0";
            }

            if (o is Enum e)
            {
                return ((int)(object)e).ToString();
            }

            return o.ToString();
        }

        public string GenerateInsert<T>(T toInsert) where T : class
        {
            if (toInsert is null)
            {
                throw new ArgumentNullException(nameof(toInsert));
            }

            Type objectType = toInsert.GetType();

            StringBuilder stringBuilder = new();

            stringBuilder.Append($"INSERT INTO [dbo].[{objectType.Name}] (");

            stringBuilder.Append(this.PropertyNameList(objectType, true));

            stringBuilder.Append(')');

            if (this.TryGetKey(objectType, out PropertyInfo keyProperty))
            {
                stringBuilder.Append($" output INSERTED.{keyProperty.Name} ");
            }

            stringBuilder.Append(" VALUES (");

            stringBuilder.Append(this.PropertyValueList(objectType, toInsert, true));

            stringBuilder.Append(')');

            return stringBuilder.ToString();
        }

        private IEnumerable<PropertyInfo> GetMappedProperties(Type type, bool skipKey)
        {
            PropertyInfo key = null;

            if (skipKey)
            {
                _ = this.TryGetKey(type, out key);
            }

            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property == key)
                {
                    continue;
                }

                if (property.GetGetMethod() is null)
                {
                    continue;
                }

                if (property.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    continue;
                }

                yield return property;
            }
        }

        private string PropertyNameList(Type type, bool skipKey)
        {
            StringBuilder sb = new();

            PropertyInfo[] properties = this.GetMappedProperties(type, skipKey).ToArray();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo pi = properties[i];

                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append($"[{pi.Name}]");
            }

            return sb.ToString();
        }

        private string PropertyValueList(Type type, object instance, bool skipKey)
        {
            StringBuilder sb = new();

            PropertyInfo[] properties = this.GetMappedProperties(type, skipKey).ToArray();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo pi = properties[i];

                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(this.FormatArgument(pi.GetValue(instance)));
            }

            return sb.ToString();
        }

        private bool TryGetKey(Type type, out PropertyInfo keyProperty)
        {
            PropertyInfo[] properties = type.GetProperties().Where(p => p.GetGetMethod() != null).ToArray();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo pi = properties[i];

                if (pi.GetCustomAttribute<KeyAttribute>() != null)
                {
                    keyProperty = pi;
                    return true;
                }
            }

            keyProperty = null;
            return false;
        }
    }
}