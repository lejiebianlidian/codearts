﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 参数项目。
    /// </summary>
    public class ParameterItem : StoreItem
    {
        private static readonly ConcurrentDictionary<ParameterInfo, ParameterItem> ItemCache = new ConcurrentDictionary<ParameterInfo, ParameterItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">参数。</param>
        private ParameterItem(ParameterInfo info) : base(info)
        {
            Member = info;
        }

        /// <summary>
        /// 参数名称。
        /// </summary>
        public override string Name => Member.Name;

        /// <summary>
        /// 参数信息。
        /// </summary>
        public ParameterInfo Member { get; }

        /// <summary>
        /// 可选参数。
        /// </summary>
        public bool IsOptional => Member.IsOptional;

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 是否有默认值。
        /// </summary>
        public bool HasDefaultValue => Member.HasDefaultValue;
#endif

        /// <summary>
        /// 默认值。
        /// </summary>
        public object DefaultValue => Member.DefaultValue;

        /// <summary>
        /// 参数类型。
        /// </summary>
        public Type ParameterType => Member.ParameterType;

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="info">信息。</param>
        /// <returns></returns>
        public static ParameterItem Get(ParameterInfo info) => ItemCache.GetOrAdd(info, parameterInfo => new ParameterItem(parameterInfo));
    }
}
