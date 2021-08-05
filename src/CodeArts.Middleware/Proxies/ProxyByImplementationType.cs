﻿using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Proxies
{
    class ProxyByImplementationType : IProxyByPattern
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);

        private readonly ModuleEmitter moduleEmitter;
        private readonly Type serviceType;
        private readonly Type implementationType;
        private readonly ServiceLifetime lifetime;

        public ProxyByImplementationType(ModuleEmitter moduleEmitter, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            this.moduleEmitter = moduleEmitter;
            this.serviceType = serviceType;
            this.implementationType = implementationType;
            this.lifetime = lifetime;
        }

        public ServiceDescriptor Resolve()
        {
            if (serviceType.IsSealed)
            {
                throw new NotSupportedException("无法代理密封类!");
            }

            if (serviceType.IsInterface)
            {
                return ResolveIsInterface();
            }
            else if (implementationType.IsSealed)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是密封类!");
            }
            else if (implementationType.IsInterface)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是接口!");
            }
            else if (implementationType.IsAbstract)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是抽象类!");
            }
            else if (implementationType.IsValueType)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是值类型!");
            }
            else
            {
                return ResolveIsClass();
            }
        }

        private ServiceDescriptor ResolveIsInterface()
        {
            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = serviceType.GetAllInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, interfaces);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new AstExpression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.Append(Assign(instanceAst, Convert(New(constructorInfo, parameterEmiters), serviceType)));
            }

            var interceptMethods = new Dictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance);

            foreach (var type in interfaces)
            {
                bool flag = type.IsDefined(InterceptAttributeType, false);

                var attributes = flag
                    ? (InterceptAttribute[])type.GetCustomAttributes(InterceptAttributeType, false)
                    : new InterceptAttribute[0];

                foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var interceptAttributes = methodInfo.IsDefined(InterceptAttributeType, false)
                        ? Merge(attributes, (InterceptAttribute[])methodInfo.GetCustomAttributes(InterceptAttributeType, false))
                        : attributes;

                    if (interceptAttributes.Length == 0)
                    {
                        continue;
                    }

                    if (interceptMethods.TryGetValue(methodInfo, out var intercepts))
                    {
                        interceptMethods[methodInfo] = Merge(intercepts, interceptAttributes);
                    }
                    else
                    {
                        interceptMethods.Add(methodInfo, interceptAttributes);
                    }
                }
            }

            var propertyMethods = new HashSet<MethodInfo>();

            foreach (var propertyInfo in serviceType.GetProperties())
            {
                var propertyEmitter = classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType);

                if (propertyInfo.CanRead)
                {
                    var readMethod = propertyInfo.GetGetMethod(true);

                    propertyMethods.Add(readMethod);

                    propertyEmitter.SetGetMethod(InterceptCore.DefineMethodOverride(instanceAst, classEmitter, readMethod, null));
                }

                if (propertyInfo.CanWrite)
                {
                    var writeMethod = propertyInfo.GetSetMethod(true);

                    propertyMethods.Add(writeMethod);

                    propertyEmitter.SetSetMethod(InterceptCore.DefineMethodOverride(instanceAst, classEmitter, writeMethod, null));
                }
            }

            foreach (var methodInfo in serviceType.GetMethods())
            {
                if (propertyMethods.Contains(methodInfo))
                {
                    continue;
                }

                if (interceptMethods.TryGetValue(methodInfo, out var interceptAttributes))
                {
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, interceptAttributes);
                }
                else
                {
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, null);
                }
            }

            propertyMethods.Clear();

            interceptMethods.Clear();

            return new ServiceDescriptor(serviceType, classEmitter.CreateType(), lifetime);
        }

        private static T[] Merge<T>(T[] arrays, T[] arrays2)
        {
            if (arrays.Length == 0)
            {
                return arrays2;
            }

            if (arrays2.Length == 0)
            {
                return arrays;
            }

            var results = new T[arrays.Length + arrays2.Length];

            System.Array.Copy(arrays, results, arrays.Length);

            System.Array.Copy(arrays2, 0, results, arrays.Length, arrays2.Length);

            return results;
        }

        private ServiceDescriptor ResolveIsClass()
        {
            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = implementationType.GetInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, implementationType, interfaces);

            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new ParameterEmitter[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.InvokeBaseConstructor(constructorInfo, parameterEmiters);
            }

            var interceptMethods = new Dictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance);

            foreach (var type in interfaces)
            {
                bool flag = type.IsDefined(InterceptAttributeType, false);

                var attributes = flag
                    ? (InterceptAttribute[])type.GetCustomAttributes(InterceptAttributeType, false)
                    : new InterceptAttribute[0];

                foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var interceptAttributes = methodInfo.IsDefined(InterceptAttributeType, false)
                        ? Merge(attributes, (InterceptAttribute[])methodInfo.GetCustomAttributes(InterceptAttributeType, false))
                        : attributes;

                    if (interceptAttributes.Length == 0)
                    {
                        continue;
                    }

                    if (interceptMethods.TryGetValue(methodInfo, out var intercepts))
                    {
                        interceptMethods[methodInfo] = Merge(intercepts, interceptAttributes);
                    }
                    else
                    {
                        interceptMethods.Add(methodInfo, interceptAttributes);
                    }
                }
            }

            var iterationType = serviceType;

            do
            {
                bool isDefined = iterationType.IsDefined(InterceptAttributeType, false);

                var intercepts = isDefined
                    ? (InterceptAttribute[])iterationType.GetCustomAttributes(InterceptAttributeType, false)
                    : new InterceptAttribute[0];

                foreach (var methodInfo in iterationType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var interceptAttributes = methodInfo.IsDefined(InterceptAttributeType, false)
                        ? Merge(intercepts, (InterceptAttribute[])methodInfo.GetCustomAttributes(InterceptAttributeType, false))
                        : intercepts;

                    if (interceptAttributes.Length == 0)
                    {
                        continue;
                    }

                    if (interceptMethods.TryGetValue(methodInfo, out var attributes))
                    {
                        interceptMethods[methodInfo] = Merge(interceptAttributes, attributes);
                    }
                    else
                    {
                        interceptMethods.Add(methodInfo, interceptAttributes);
                    }
                }

                iterationType = iterationType.BaseType;

            } while (iterationType != null && iterationType != typeof(object));

            foreach (var methodInfo in serviceType.GetMethods())
            {
                if (interceptMethods.TryGetValue(methodInfo, out var interceptAttributes))
                {
                    InterceptCore.DefineMethodOverride(This, classEmitter, methodInfo, interceptAttributes);
                }
            }

            interceptMethods.Clear();

            return new ServiceDescriptor(serviceType, classEmitter.CreateType(), lifetime);
        }
    }
}
