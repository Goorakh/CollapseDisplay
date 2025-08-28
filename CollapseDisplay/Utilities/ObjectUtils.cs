using System;
using System.Reflection;

namespace CollapseDisplay.Utilities
{
    public static class ObjectUtils
    {
        public static T ShallowCopy<T>(T original, BindingFlags fieldCopyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            if (typeof(T).IsValueType || original == null)
                return original;

            if (original is ICloneable cloneable)
            {
                return (T)cloneable.Clone();
            }

            T copy = Activator.CreateInstance<T>();

            foreach (FieldInfo field in typeof(T).GetFields(fieldCopyFlags))
            {
                try
                {
                    object fieldValue = field.GetValue(original);
                    field.SetValue(copy, fieldValue);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to copy field value {field.DeclaringType.FullName}.{field.Name}: {e}");
                }
            }

            return copy;
        }
    }
}
