using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 类型转换。
    /// </summary>
    [DebuggerDisplay("{body} as {RuntimeType.Name}")]
    public class TypeAsAst : AstExpression
    {
        private readonly AstExpression body;


        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">成员。</param>
        /// <param name="type">类型。</param>
        public TypeAsAst(AstExpression body, Type type) : base(type)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (RuntimeType.IsNullable())
            {
                body.Load(ilg);

                if (body.RuntimeType.IsValueType)
                {
                    ilg.Emit(OpCodes.Box, body.RuntimeType);
                }

                ilg.Emit(OpCodes.Isinst, Nullable.GetUnderlyingType(RuntimeType));
            }
            else
            {
                body.Load(ilg);

                if (body.RuntimeType.IsValueType)
                {
                    ilg.Emit(OpCodes.Box, body.RuntimeType);
                }

                ilg.Emit(OpCodes.Isinst, RuntimeType);
            }
        }
    }
}
