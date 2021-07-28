using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("catch({variable}){ {body} }")]
    public class CatchAst : AstExpression
    {
        private class CatchBlockAst : AstExpression
        {
            public CatchBlockAst(Type returnType) : base(returnType)
            {
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.BeginCatchBlock(RuntimeType);
            }
        }

        private readonly AstExpression body;
        private readonly Type exceptionType;
        private readonly VariableAst variable;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="catchAst">异常模块。</param>
        protected CatchAst(CatchAst catchAst) : base(catchAst?.RuntimeType)
        {
            if (catchAst is null)
            {
                throw new ArgumentNullException(nameof(catchAst));
            }

            body = catchAst.body;
            variable = catchAst.variable;
            exceptionType = catchAst.exceptionType;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        public CatchAst(AstExpression body) : this(body, typeof(Exception)) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        /// <param name="exceptionType">异常类型。</param>
        public CatchAst(AstExpression body, Type exceptionType) : this(body, exceptionType, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        /// <param name="variable">变量。</param>
        public CatchAst(AstExpression body, VariableAst variable) : this(body, typeof(Exception), variable)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        /// <param name="exceptionType">异常类型。</param>
        /// <param name="variable">变量。</param>
        public CatchAst(AstExpression body, Type exceptionType, VariableAst variable) : base(body.RuntimeType)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));

            this.exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));

            if (variable is null || variable.RuntimeType == exceptionType)
            {
                this.variable = variable;
            }
            else
            {
                throw new AstException($"变量类型“{variable.RuntimeType}”和异常类型“{exceptionType}”不一致!");
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="body">内容。</param>
        protected virtual void Emit(ILGenerator ilg, AstExpression body)
        {
            body.Load(ilg);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (variable is null)
            {
                ilg.BeginCatchBlock(exceptionType);
            }
            else
            {
                variable.Assign(ilg, new CatchBlockAst(exceptionType));
            }

            ilg.Emit(OpCodes.Nop);

            Emit(ilg, body);
        }
    }
}
