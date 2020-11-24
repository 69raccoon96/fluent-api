using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting
{
    public class PrintingConfig<TOwner>
    {
        private readonly Config config;
        private PropertyInfo selectedProperty;

        public PrintingConfig()
        {
            config = new Config();
        }

        public string PrintToString(TOwner obj)
        {
            return PrintToString(obj, 0);
        }

        public PrintingConfig<TOwner> TypeSerializer<TProperty>(Func<object, string> func)
        {
            config.TypesSerializer.Add(typeof(TProperty), func);
            return this;
        }

        public PrintingConfig<TOwner> Exclude<TProperty>()
        {
            config.ExcludedTypes.Add(typeof(TProperty));
            return this;
        }

        public SelectedProperty<TOwner, TProperty> Choose<TProperty>(Expression<Func<TOwner, TProperty>> selector)
        {
            selectedProperty = (PropertyInfo) ((MemberExpression) selector.Body).Member;
            return new SelectedProperty<TOwner, TProperty>(selectedProperty, this, config);
        }

        private string PrintToString(object obj, int nestingLevel)
        {
            //TODO apply configurations
            if (obj == null)
                return "null" + Environment.NewLine;

            var finalTypes = new[]
            {
                typeof(int), typeof(double), typeof(float), typeof(string),
                typeof(DateTime), typeof(TimeSpan)
            };

            if (finalTypes.Contains(obj.GetType()))
                return obj + Environment.NewLine;

            var identation = new string('\t', nestingLevel + 1);
            var sb = new StringBuilder();
            var type = obj.GetType();
            sb.AppendLine(type.Name);
            foreach (var propertyInfo in type.GetProperties())
            {
                if (config.IsExcluded(propertyInfo))
                    continue;

                sb.Append(identation + propertyInfo.Name + " = ");

                var lineToAdd = SerializeProperty(propertyInfo, obj) ?? (SerializeType(propertyInfo, obj)
                                                                         ?? DefaultSerialization(propertyInfo, obj));
                sb.Append(lineToAdd);
            }

            return sb.ToString();
        }

        private string SerializeProperty(PropertyInfo propertyInfo, object obj)
        {
            var current = propertyInfo.GetValue(obj);
            return config.IsSpecialSerialize(propertyInfo, current, out var result) ? result : null;
        }

        private string SerializeType(PropertyInfo propertyInfo, object obj)
        {
            var current = propertyInfo.GetValue(obj);
            return config.IsSpecialSerialize(propertyInfo.PropertyType, current, out var result) ? result : null;
        }

        private string DefaultSerialization(PropertyInfo propertyInfo, object obj)
        {
            return PrintToString(propertyInfo.GetValue(obj), 1);
        }
    }
}