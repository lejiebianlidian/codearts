using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("try \\{ //TODO:somethings \\}")]
    public class TryAst : BlockAst
    {
        private readonly AstExpression finallyAst;
        private readonly List<CatchAst> catchAsts;

        private class VBlockAst : BlockAst
        {
            private readonly LocalBuilder variable;
            private readonly Label label;

            public VBlockAst(LocalBuilder variable, Label label, BlockAst blockAst) : base(blockAst)
            {
                this.variable = variable;
                this.label = label;
            }

            protected override void Emit(ILGenerator ilg) => Emit(ilg, variable, label);
            protected override void EmitVoid(ILGenerator ilg) => EmitVoid(ilg, label);
        }

        //? 忽略返回值。
        private class VoidBlockAst : BlockAst
        {
            private readonly Label label;

            public VoidBlockAst(BlockAst blockAst, Label label) : base(blockAst)
            {
                this.label = label;
            }

            protected override void Emit(ILGenerator ilg) => EmitVoid(ilg, label);
            protected override void EmitVoid(ILGenerator ilg) => EmitVoid(ilg, label);
        }

        private class VCatchAst : CatchAst
        {
            private readonly LocalBuilder variable;
            private readonly Label label;

            public VCatchAst(LocalBuilder variable, Label label, CatchAst catchAst) : base(catchAst)
            {
                this.variable = variable;
                this.label = label;
            }

            protected override void Emit(ILGenerator ilg, AstExpression body)
            {
                if (body is ReturnAst returnAst)
                {
                    base.Emit(ilg, returnAst.Unbox());
                }
                else if (body is BlockAst blockAst)
                {
                    base.Emit(ilg, new VBlockAst(variable, label, blockAst));
                }
                else
                {
                    base.Emit(ilg, body);
                }
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="tryAst">异常捕获。</param>
        protected TryAst(TryAst tryAst) : base(tryAst)
        {
            catchAsts = tryAst.catchAsts;
            finallyAst = tryAst.finallyAst;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        public TryAst(Type returnType) : base(returnType)
        {
            catchAsts = new List<CatchAst>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        /// <param name="finallyAst">一定会执行的代码。</param>
        public TryAst(Type returnType, AstExpression finallyAst) : base(returnType)
        {
            this.finallyAst = finallyAst ?? throw new ArgumentNullException(nameof(finallyAst));

            catchAsts = new List<CatchAst>();
        }

        /// <summary>
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns></returns>
        public override BlockAst Append(AstExpression code)
        {
            if (code is CatchAst catchAst)
            {
                if (RuntimeType.IsAssignableFrom(catchAst.RuntimeType) || typeof(Exception).IsAssignableFrom(catchAst.RuntimeType))
                {
                    catchAsts.Add(catchAst);
                }
                else
                {
                    throw new ArgumentException("捕获器只能返回相同类型或抛出异常!", nameof(code));
                }

                return this;
            }

            return base.Append(code);
        }

        /// <summary>
        /// 发行变量和代码块(无返回值)。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="label">跳转位置。</param>
        protected override void EmitVoid(ILGenerator ilg, Label label)
        {
            ilg.BeginExceptionBlock();

            base.EmitVoid(ilg, label);

            if (catchAsts.Count > 0)
            {
                foreach (var item in catchAsts)
                {
                    item.Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            if (finallyAst != null)
            {
                ilg.BeginFinallyBlock();

                if (finallyAst.RuntimeType == typeof(void))
                {
                    finallyAst.Load(ilg);
                }
                else if (finallyAst is BlockAst blockAst)
                {
                    new VoidBlockAst(blockAst, label)
                        .Load(ilg);
                }
                else
                {
                    finallyAst.Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            ilg.EndExceptionBlock();
        }

        /// <summary>
        /// 发行变量和代码块（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="variable">存储结果的变量。</param>
        /// <param name="label">跳转位置。</param>
        protected override void Emit(ILGenerator ilg, LocalBuilder variable, Label label)
        {
            ilg.BeginExceptionBlock();

            base.Emit(ilg, variable, label);

            if (catchAsts.Count > 0)
            {
                foreach (var catchAst in catchAsts)
                {
                    new VCatchAst(variable, label, catchAst)
                        .Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            if (finallyAst != null)
            {
                ilg.BeginFinallyBlock();

                if (finallyAst.RuntimeType == typeof(void))
                {
                    finallyAst.Load(ilg);
                }
                else if (finallyAst is BlockAst blockAst)
                {
                    new VoidBlockAst(blockAst, label)
                        .Load(ilg);
                }
                else
                {
                    finallyAst.Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            ilg.EndExceptionBlock();
        }

        /// <summary>
        /// 发行（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected override void Emit(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            var variable = ilg.DeclareLocal(RuntimeType);

            Emit(ilg, variable, label);

            ilg.MarkLabel(label);

            ilg.Emit(OpCodes.Ldloc, variable);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (catchAsts.Count == 0 && finallyAst is null)
            {
                throw new AstException("表达式残缺，未设置“catch”代码块和“finally”代码块至少设置其一！");
            }

            base.Load(ilg);
        }
    }
}
