using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("try \\{ {body} \\}")]
    public class TryAst : BlockAst
    {
        private static readonly Label EmptyLabel = default;

        private readonly List<CatchAst> catchAsts;
        private readonly List<FinallyAst> finallyAsts;

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
                    base.Emit(ilg, returnAst.OnlyBodyAst());
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
            finallyAsts = tryAst.finallyAsts;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        public TryAst(Type returnType) : base(returnType)
        {
            catchAsts = new List<CatchAst>();
            finallyAsts = new List<FinallyAst>();
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

            if (code is FinallyAst finallyAst)
            {
                finallyAsts.Add(finallyAst);

                return this;
            }

            return base.Append(code);
        }

        /// <summary>
        /// 发行变量和代码块(无返回值)。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected override void EmitVoid(ILGenerator ilg)
        {
            ilg.BeginExceptionBlock();

            base.EmitVoid(ilg);

            if (catchAsts.Count > 0)
            {
                foreach (var item in catchAsts)
                {
                    item.Load(ilg);
                }
            }

            if (finallyAsts.Count > 0)
            {
                ilg.BeginFinallyBlock();

                ilg.Emit(OpCodes.Nop);

                foreach (var item in finallyAsts)
                {
                    item.Load(ilg);
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
        /// <param name="label"></param>
        protected override void Emit(ILGenerator ilg, LocalBuilder variable, Label label)
        {
            var labelCatch = ilg.DefineLabel();

            ilg.BeginExceptionBlock();

            base.Emit(ilg, variable, labelCatch);

            if (catchAsts.Count > 0)
            {
                ilg.Emit(OpCodes.Nop);

                foreach (var catchAst in catchAsts)
                {
                    new VCatchAst(variable, labelCatch, catchAst)
                        .Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            ilg.MarkLabel(labelCatch);

            if (finallyAsts.Count > 0)
            {
                ilg.BeginFinallyBlock();

                ilg.Emit(OpCodes.Nop);

                foreach (var item in finallyAsts)
                {
                    item.Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);
            }

            ilg.EndExceptionBlock();

            if (label != EmptyLabel)
            {
                ilg.Emit(OpCodes.Leave_S, label);
            }
        }

        /// <summary>
        /// 发行（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected override void Emit(ILGenerator ilg)
        {
            var variable = ilg.DeclareLocal(RuntimeType);

            Emit(ilg, variable, EmptyLabel);

            ilg.Emit(OpCodes.Ldloc, variable);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (catchAsts.Count == 0 && finallyAsts.Count == 0)
            {
                throw new AstException("表达式残缺，未设置捕获代码块或最终执行代码块！");
            }

            base.Load(ilg);
        }
    }
}
