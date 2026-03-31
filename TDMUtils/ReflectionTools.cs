using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace TDMUtils
{
    public static class ReflectionTools
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags AllFlags = InstanceFlags | StaticFlags;

        private static readonly ConcurrentDictionary<string, PropertyInfo?> _propertyCache = new();
        private static readonly ConcurrentDictionary<string, FieldInfo?> _fieldCache = new();
        private static readonly ConcurrentDictionary<string, MethodInfo?> _methodCache = new();

        private static string BuildKey(Type type, string name, BindingFlags flags)
            => $"{type.AssemblyQualifiedName}|{name}|{(int)flags}";

        private static string BuildMethodKey(Type type, string name, BindingFlags flags, Type[]? parameterTypes)
        {
            string parameters = parameterTypes == null || parameterTypes.Length == 0
                ? ""
                : string.Join(",", parameterTypes.Select(t => t.AssemblyQualifiedName ?? t.FullName ?? t.Name));

            return $"{type.AssemblyQualifiedName}|{name}|{(int)flags}|{parameters}";
        }
        /// <summary>
        /// Determines whether the specified object contains a property with the given name.
        /// Supports public and private instance/static properties and dynamic ExpandoObject members.
        /// </summary>
        /// <param name="obj">The object to inspect.</param>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <param name="includePrivate">True to include non-public properties; otherwise, only public properties are considered.</param>
        /// <returns>True if the property exists on the object; otherwise, false.</returns>
        public static bool HasProperty(object obj, string propertyName, bool includePrivate = true)
        {
            if (obj is null || string.IsNullOrWhiteSpace(propertyName))
                return false;

            if (obj is ExpandoObject e)
                return ((IDictionary<string, object>)e).ContainsKey(propertyName);

            return GetPropertyInfo(obj.GetType(), propertyName, includePrivate) != null;
        }
        /// <summary>
        /// Determines whether the specified object contains a field with the given name.
        /// Supports public and private instance/static fields.
        /// </summary>
        /// <param name="obj">The object to inspect.</param>
        /// <param name="fieldName">The name of the field to check.</param>
        /// <param name="includePrivate">True to include non-public fields; otherwise, only public fields are considered.</param>
        /// <returns>True if the field exists on the object; otherwise, false.</returns>
        public static bool HasField(object obj, string fieldName, bool includePrivate = true)
        {
            if (obj is null || string.IsNullOrWhiteSpace(fieldName))
                return false;

            return GetFieldInfo(obj.GetType(), fieldName, includePrivate) != null;
        }
        /// <summary>
        /// Determines whether the specified object contains a method with the given name and optional parameter types.
        /// Supports public and private instance/static methods.
        /// </summary>
        /// <param name="obj">The object to inspect.</param>
        /// <param name="methodName">The name of the method to check.</param>
        /// <param name="includePrivate">True to include non-public methods; otherwise, only public methods are considered.</param>
        /// <param name="parameterTypes">Optional parameter types to match a specific overload.</param>
        /// <returns>True if a matching method exists on the object; otherwise, false.</returns>
        public static bool HasMethod(object obj, string methodName, bool includePrivate = true, params Type[] parameterTypes)
        {
            if (obj is null || string.IsNullOrWhiteSpace(methodName))
                return false;

            return GetMethodInfo(obj.GetType(), methodName, includePrivate, parameterTypes) != null;
        }

        /// <summary>
        /// Retrieves cached metadata for a property on the specified type.
        /// Includes public and optionally private instance/static properties.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="includePrivate">True to include non-public properties; otherwise, only public properties are considered.</param>
        /// <returns>The <see cref="PropertyInfo"/> if found; otherwise, null.</returns>
        public static PropertyInfo? GetPropertyInfo(Type type, string propertyName, bool includePrivate = true)
        {
            if (type == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            BindingFlags flags = includePrivate ? AllFlags : BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            string key = BuildKey(type, propertyName, flags);

            return _propertyCache.GetOrAdd(key, _ => type.GetProperty(propertyName, flags));
        }
        /// <summary>
        /// Retrieves cached metadata for a field on the specified type.
        /// Includes public and optionally private instance/static fields.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="includePrivate">True to include non-public fields; otherwise, only public fields are considered.</param>
        /// <returns>The <see cref="FieldInfo"/> if found; otherwise, null.</returns>
        public static FieldInfo? GetFieldInfo(Type type, string fieldName, bool includePrivate = true)
        {
            if (type == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            BindingFlags flags = includePrivate ? AllFlags : BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            string key = BuildKey(type, fieldName, flags);

            return _fieldCache.GetOrAdd(key, _ => type.GetField(fieldName, flags));
        }
        /// <summary>
        /// Retrieves cached metadata for a method on the specified type, optionally matching parameter types.
        /// Includes public and optionally private instance/static methods.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="includePrivate">True to include non-public methods; otherwise, only public methods are considered.</param>
        /// <param name="parameterTypes">Optional parameter types to select a specific overload.</param>
        /// <returns>The <see cref="MethodInfo"/> if found; otherwise, null.</returns>
        public static MethodInfo? GetMethodInfo(Type type, string methodName, bool includePrivate = true, params Type[] parameterTypes)
        {
            if (type == null || string.IsNullOrWhiteSpace(methodName))
                return null;

            BindingFlags flags = includePrivate ? AllFlags : BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            string key = BuildMethodKey(type, methodName, flags, parameterTypes);

            return _methodCache.GetOrAdd(key, _ =>
            {
                if (parameterTypes != null && parameterTypes.Length > 0)
                    return type.GetMethod(methodName, flags, null, parameterTypes, null);

                return type.GetMethod(methodName, flags);
            });
        }
        /// <summary>
        /// Gets the value of a property from the specified object.
        /// Supports public and private properties and dynamic ExpandoObject members.
        /// </summary>
        /// <param name="obj">The object containing the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="includePrivate">True to include non-public properties; otherwise, only public properties are considered.</param>
        /// <returns>The property value if found; otherwise, null.</returns>
        public static object? GetPropertyValue(object obj, string propertyName, bool includePrivate = true)
        {
            if (obj is null)
                return null;

            if (obj is ExpandoObject e &&
                ((IDictionary<string, object>)e).TryGetValue(propertyName, out object? expandoValue))
                return expandoValue;

            PropertyInfo? property = GetPropertyInfo(obj.GetType(), propertyName, includePrivate);
            return property?.GetValue(obj);
        }

        /// <summary>
        /// Gets the value of a property from the specified object and attempts to cast it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="obj">The object containing the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="includePrivate">True to include non-public properties; otherwise, only public properties are considered.</param>
        /// <returns>The property value cast to <typeparamref name="T"/> if successful; otherwise, the default value of <typeparamref name="T"/>.</returns>
        public static T? GetPropertyValue<T>(object obj, string propertyName, bool includePrivate = true)
        {
            object? value = GetPropertyValue(obj, propertyName, includePrivate);
            return value is T typed ? typed : default;
        }
        /// <summary>
        /// Sets the value of a property on the specified object.
        /// Supports public and private writable properties and dynamic ExpandoObject members.
        /// </summary>
        /// <param name="obj">The object containing the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value to assign.</param>
        /// <param name="includePrivate">True to include non-public properties; otherwise, only public properties are considered.</param>
        /// <returns>True if the property was found and set; otherwise, false.</returns>
        public static bool SetPropertyValue(object obj, string propertyName, object? value, bool includePrivate = true)
        {
            if (obj is null)
                return false;

            if (obj is ExpandoObject e)
            {
                ((IDictionary<string, object>)e)[propertyName] = value!;
                return true;
            }

            PropertyInfo? property = GetPropertyInfo(obj.GetType(), propertyName, includePrivate);
            if (property == null || !property.CanWrite)
                return false;

            property.SetValue(obj, value);
            return true;
        }
        /// <summary>
        /// Gets the value of a field from the specified object.
        /// Supports public and private fields.
        /// </summary>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="includePrivate">True to include non-public fields; otherwise, only public fields are considered.</param>
        /// <returns>The field value if found; otherwise, null.</returns>
        public static object? GetFieldValue(object obj, string fieldName, bool includePrivate = true)
        {
            if (obj is null)
                return null;

            FieldInfo? field = GetFieldInfo(obj.GetType(), fieldName, includePrivate);
            return field?.GetValue(obj);
        }

        /// <summary>
        /// Gets the value of a field from the specified object and attempts to cast it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="includePrivate">True to include non-public fields; otherwise, only public fields are considered.</param>
        /// <returns>The field value cast to <typeparamref name="T"/> if successful; otherwise, the default value of <typeparamref name="T"/>.</returns>
        public static T? GetFieldValue<T>(object obj, string fieldName, bool includePrivate = true)
        {
            object? value = GetFieldValue(obj, fieldName, includePrivate);
            return value is T typed ? typed : default;
        }
        /// <summary>
        /// Sets the value of a field on the specified object.
        /// Supports public and private fields.
        /// </summary>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value to assign.</param>
        /// <param name="includePrivate">True to include non-public fields; otherwise, only public fields are considered.</param>
        /// <returns>True if the field was found and set; otherwise, false.</returns>
        public static bool SetFieldValue(object obj, string fieldName, object? value, bool includePrivate = true)
        {
            if (obj is null)
                return false;

            FieldInfo? field = GetFieldInfo(obj.GetType(), fieldName, includePrivate);
            if (field == null)
                return false;

            field.SetValue(obj, value);
            return true;
        }

        /// <summary>
        /// Invokes a method on the specified object using reflection.
        /// Supports public and private instance/static methods and overload resolution.
        /// </summary>
        /// <param name="obj">The target object.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="includePrivate">True to include non-public methods; otherwise, only public methods are considered.</param>
        /// <param name="args">Optional arguments to pass to the method.</param>
        /// <returns>The return value of the method if successful; otherwise, null.</returns>
        public static object? InvokeMethod(object obj, string methodName, bool includePrivate = true, params object?[]? args)
        {
            if (obj is null || string.IsNullOrWhiteSpace(methodName))
                return null;

            Type[] parameterTypes = args?
                .Select(a => a?.GetType() ?? typeof(object))
                .ToArray() ?? Array.Empty<Type>();

            MethodInfo? method = GetMethodInfo(obj.GetType(), methodName, includePrivate, parameterTypes);

            if (method == null)
            {
                method = GetCompatibleMethod(obj.GetType(), methodName, includePrivate, args);
                if (method == null)
                    return null;
            }

            return method.Invoke(obj, args);
        }

        /// <summary>
        /// Invokes a method on the specified object and attempts to cast the return value to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="obj">The target object.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="includePrivate">True to include non-public methods; otherwise, only public methods are considered.</param>
        /// <param name="args">Optional arguments to pass to the method.</param>
        /// <returns>The return value cast to <typeparamref name="T"/> if successful; otherwise, the default value of <typeparamref name="T"/>.</returns>
        public static T? InvokeMethod<T>(object obj, string methodName, bool includePrivate = true, params object?[]? args)
        {
            object? value = InvokeMethod(obj, methodName, includePrivate, args);
            return value is T typed ? typed : default;
        }

        /// <summary>
        /// Gets the value of a field or property with the specified name from the object.
        /// Searches fields first, then properties. Supports public and private members.
        /// </summary>
        /// <param name="obj">The object containing the member.</param>
        /// <param name="memberName">The name of the field or property.</param>
        /// <param name="includePrivate">True to include non-public members; otherwise, only public members are considered.</param>
        /// <returns>The member value if found; otherwise, null.</returns>
        public static object? GetMemberValue(object obj, string memberName, bool includePrivate = true)
        {
            if (obj is null || string.IsNullOrWhiteSpace(memberName))
                return null;

            FieldInfo? field = GetFieldInfo(obj.GetType(), memberName, includePrivate);
            if (field != null)
                return field.GetValue(obj);

            PropertyInfo? property = GetPropertyInfo(obj.GetType(), memberName, includePrivate);
            if (property != null)
                return property.GetValue(obj);

            return null;
        }
        /// <summary>
        /// Sets the value of a field or property with the specified name on the object.
        /// Searches fields first, then properties. Supports public and private members.
        /// </summary>
        /// <param name="obj">The object containing the member.</param>
        /// <param name="memberName">The name of the field or property.</param>
        /// <param name="value">The value to assign.</param>
        /// <param name="includePrivate">True to include non-public members; otherwise, only public members are considered.</param>
        /// <returns>True if the member was found and set; otherwise, false.</returns>
        public static bool SetMemberValue(object obj, string memberName, object? value, bool includePrivate = true)
        {
            if (obj is null || string.IsNullOrWhiteSpace(memberName))
                return false;

            FieldInfo? field = GetFieldInfo(obj.GetType(), memberName, includePrivate);
            if (field != null)
            {
                field.SetValue(obj, value);
                return true;
            }

            PropertyInfo? property = GetPropertyInfo(obj.GetType(), memberName, includePrivate);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
                return true;
            }

            return false;
        }
        /// <summary>
        /// Determines whether the object contains a field or property with the specified name.
        /// Supports public and private members.
        /// </summary>
        /// <param name="obj">The object to inspect.</param>
        /// <param name="memberName">The name of the field or property.</param>
        /// <param name="includePrivate">True to include non-public members; otherwise, only public members are considered.</param>
        /// <returns>True if a matching member exists; otherwise, false.</returns>
        public static bool HasMember(object obj, string memberName, bool includePrivate = true)
        {
            if (obj is null || string.IsNullOrWhiteSpace(memberName))
                return false;

            return HasField(obj, memberName, includePrivate)
                || HasProperty(obj, memberName, includePrivate);
        }
        /// <summary>
        /// Clears all cached reflection metadata stored by this utility class.
        /// </summary>
        public static void ClearCache()
        {
            _propertyCache.Clear();
            _fieldCache.Clear();
            _methodCache.Clear();
        }

        private static MethodInfo? GetCompatibleMethod(Type type, string methodName, bool includePrivate, object?[]? args)
        {
            BindingFlags flags = includePrivate ? AllFlags : BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            MethodInfo[] methods = type.GetMethods(flags).Where(m => m.Name == methodName).ToArray();
            int argCount = args?.Length ?? 0;

            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != argCount)
                    continue;

                bool match = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    object? arg = args![i];
                    Type parameterType = parameters[i].ParameterType;

                    if (arg == null)
                        continue;

                    if (!parameterType.IsInstanceOfType(arg) &&
                        !(parameterType.IsAssignableFrom(arg.GetType())))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return method;
            }

            return null;
        }

        public static T[] CreateAllImplementations<T>(params Assembly[]? assemblies) where T : class
        {
            var targetType = typeof(T);
            var source = assemblies?.Length > 0 ? assemblies : AppDomain.CurrentDomain.GetAssemblies();

            return [.. source.SelectMany(a => a.GetTypes())
                .Where(t => targetType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(Activator.CreateInstance).OfType<T>()];
        }
    }
}