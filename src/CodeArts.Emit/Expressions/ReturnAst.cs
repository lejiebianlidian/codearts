﻿using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 返回。
    /// </summary>
    [DebuggerDisplay("return {body}")]
    public class ReturnAst : AstExpression
    {
        private sealed class AnyDynamic { }

        /// <summary>
        /// 任意动态类型。
        /// </summary>
        private static readonly Type AnyDynamicType = typeof(AnyDynamic);

        private readonly AstExpression body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ReturnAst() : base(AnyDynamicType) => IsEmpty = true;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">返回结果的表达式。</param>
        public ReturnAst(AstExpression body) : base(body.RuntimeType)
        {
            this.body = body;

            if (body.RuntimeType == typeof(void))
            {
                throw new ArgumentException("不能返回无返回值类型!", nameof(body));
            }
        }

        /// <summary>
        /// 未设置返回结果。
        /// </summary>
        public bool IsEmpty { get; }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            body?.Load(ilg);

            ilg.Emit(OpCodes.Ret);
        }
    }
}
