using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 代码块。
    /// </summary>
    public class BlockAst : AstExpression
    {
        private readonly List<AstExpression> codes;
        private readonly List<VariableAst> variables;

        //? 有返回值，并指定结果和跳转目标。
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

        private class VTryAst : TryAst
        {
            private readonly LocalBuilder variable;
            private readonly Label label;

            public VTryAst(LocalBuilder variable, Label label, TryAst tryAst) : base(tryAst)
            {
                this.variable = variable;
                this.label = label;
            }

            protected override void Emit(ILGenerator ilg) => Emit(ilg, variable, label);
        }

        //? 忽略返回值。
        private class VoidBlockAst : BlockAst
        {
            public VoidBlockAst(BlockAst blockAst) : base(blockAst)
            {
            }
            protected override void Emit(ILGenerator ilg) => EmitVoid(ilg);
        }

        private bool isReadOnly = false;

        /// <summary>
        /// 代码块。
        /// </summary>
        /// <param name="blockAst"></param>
        protected BlockAst(BlockAst blockAst) : base(blockAst?.RuntimeType)
        {
            if (blockAst is null)
            {
                throw new ArgumentNullException(nameof(blockAst));
            }
            codes = blockAst.codes;
            variables = blockAst.variables;
            isReadOnly = blockAst.isReadOnly;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        public BlockAst(Type returnType) : base(returnType)
        {
            codes = new List<AstExpression>();
            variables = new List<VariableAst>();
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => codes.Count == 0;

        /// <summary>
        /// 声明变量。
        /// </summary>
        /// <param name="variableType">变量类型。</param>
        /// <returns></returns>
        public VariableAst DeclareVariable(Type variableType)
        {
            if (variableType is null)
            {
                throw new ArgumentNullException(nameof(variableType));
            }

            var variable = new VariableAst(variableType);
            variables.Add(variable);
            return variable;
        }

        /// <summary>
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns></returns>
        public virtual BlockAst Append(AstExpression code)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (isReadOnly)
            {
                throw new AstException("当前代码块已作为其它代码块的一部分，不能进行修改!");
            }

            if (code is ReturnAst returnAst)
            {
                if (returnAst.IsEmpty)
                {
                    if (IsEmpty)
                    {
                        if (RuntimeType == typeof(void))
                        {
                            goto label_core;
                        }

                        throw new AstException("栈顶部无任何数据!");
                    }

#if NETSTANDARD2_1_OR_GREATER
                    AstExpression lastCode = codes[^1];
#else
                    AstExpression lastCode = codes[codes.Count - 1];
#endif

                    if (lastCode is ReturnAst)
                    {
                        return this;
                    }

                    if (lastCode.RuntimeType == RuntimeType || RuntimeType == typeof(void) || lastCode.RuntimeType.IsAssignableFrom(RuntimeType))
                    {
                        goto label_core;
                    }

                    throw new AstException($"返回类型“{lastCode.RuntimeType}”和预期的返回类型“{RuntimeType}”不相同!");
                }
                else if (RuntimeType != code.RuntimeType && code.RuntimeType.IsAssignableFrom(RuntimeType))
                {
                    throw new AstException($"返回类型“{code.RuntimeType}”和预期的返回类型“{RuntimeType}”不相同!");
                }
            }
            else if (code is BlockAst blockAst)
            {
                if (RuntimeType != code.RuntimeType && code.RuntimeType.IsAssignableFrom(RuntimeType))
                {
                    throw new AstException($"返回类型“{code.RuntimeType}”和预期的返回类型“{RuntimeType}”不相同!");
                }

                blockAst.isReadOnly = true;
            }

        label_core:

            codes.Add(code);

            return this;
        }

        /// <summary>
        /// 发行变量和代码块(无返回值)。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected virtual void EmitVoid(ILGenerator ilg)
        {
            foreach (var variable in variables)
            {
                variable.Declare(ilg);
            }

            foreach (var code in codes)
            {
                code.Load(ilg);

                if (code.RuntimeType != typeof(void))
                {
                    ilg.Emit(OpCodes.Nop);
                }
            }
        }

        /// <summary>
        /// 发行变量和代码块（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="local">存储结果的变量。</param>
        /// <param name="label"></param>
        protected virtual void Emit(ILGenerator ilg, LocalBuilder local, Label label)
        {
            if (IsEmpty)
            {
                throw new AstException("并非所有代码都有返回值!");
            }

            foreach (var variable in variables)
            {
                variable.Declare(ilg);
            }

            int i = 0, offset, len = codes.Count - 1;

            for (; i < len; i += offset)
            {
                var code = codes[i];
                var nextCode = codes[i + 1];

                if (nextCode is ReturnAst returnAst)
                {
                    offset = 2; //? 一次处理两条记录。

                    if (code is TryAst tryAst)
                    {
                        if (tryAst.RuntimeType != typeof(void))
                        {
                            code = new VTryAst(local, label, tryAst);
                        }
                    }
                    else if (code is BlockAst blockAst)
                    {
                        if (returnAst.IsEmpty)
                        {
                            code = new VBlockAst(local, label, blockAst);
                        }
                        else
                        {
                            code = new VoidBlockAst(blockAst);
                        }
                    }

                    code.Load(ilg);

                    if (returnAst.IsEmpty)
                    {
                        EmitUtils.EmitConvertToType(ilg, code.RuntimeType, local.LocalType, true);
                    }
                    else
                    {
                        returnAst.OnlyBodyAst()
                            .Load(ilg);

                        EmitUtils.EmitConvertToType(ilg, returnAst.RuntimeType, local.LocalType, true);
                    }

                    ilg.Emit(OpCodes.Stloc, local);

                    ilg.Emit(OpCodes.Leave_S, label);
                }
                else
                {
                    offset = 1;

                    if (code is BlockAst blockAst)
                    {
                        code = new VoidBlockAst(blockAst);
                    }

                    code.Load(ilg);
                }
            }

            if (i > len) //? 结尾是返回结果代码。
            {
                return;
            }

#if NETSTANDARD2_1_OR_GREATER
            var codeAst = codes[^1];
#else
            var codeAst = codes[codes.Count - 1];
#endif

            if (codeAst is TryAst tryAst)
            {
                if (tryAst.RuntimeType != typeof(void))
                {
                    codeAst = new VTryAst(local, label, tryAst);
                }
            }
            else if(codeAst is BlockAst ast)
            {
                codeAst = new VBlockAst(local, label, ast);
            }

            codeAst.Load(ilg);

            EmitUtils.EmitConvertToType(ilg, codeAst.RuntimeType, local.LocalType, true);

            ilg.Emit(OpCodes.Stloc, local);

            ilg.Emit(OpCodes.Leave_S, label);
        }

        /// <summary>
        /// 发行（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected virtual void Emit(ILGenerator ilg)
        {
            if (codes.Any(x => x is ReturnAst || x is BlockAst))
            {
                var variable = ilg.DeclareLocal(RuntimeType);

                Label label = ilg.DefineLabel();

                Emit(ilg, variable, label);

                ilg.MarkLabel(label);

                ilg.Emit(OpCodes.Ldloc, variable);
            }
            else
            {
                foreach (var variable in variables)
                {
                    variable.Declare(ilg);
                }

                foreach (var code in codes)
                {
                    code.Load(ilg);
                }
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (RuntimeType == typeof(void))
            {
                EmitVoid(ilg);
            }
            else
            {
                Emit(ilg);
            }
        }
    }
}
