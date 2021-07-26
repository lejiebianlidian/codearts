using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 代码块。
    /// </summary>
    public class BlockAst : AstExpression
    {
        private readonly List<AstExpression> codes = new List<AstExpression>();
        private readonly List<VariableAst> variables = new List<VariableAst>();

        private bool isReadOnly = false;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        public BlockAst(Type returnType) : base(returnType)
        {
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => codes.Count == 0;

        /// <summary>
        /// 最后一个是返回。
        /// </summary>
        protected internal bool HasReturn { private set; get; }

        /// <summary>
        /// 有返回。
        /// </summary>
        protected bool IsLastReturn { private set; get; }

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

            IsLastReturn = false;

            if (code is ReturnAst returnAst)
            {
                HasReturn = true;

                IsLastReturn = true;

                if (returnAst.IsEmpty)
                {
                    if (IsEmpty)
                    {
                        if (RuntimeType == typeof(void))
                        {
                            goto label_core;
                        }

                        throw new AstException("堆载顶部无任何数据!");
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
                else if (RuntimeType != code.RuntimeType)
                {
                    throw new AstException($"返回类型“{code.RuntimeType}”和预期的返回类型“{RuntimeType}”不相同!");
                }
            }
            else if (code is BlockAst blockAst)
            {
                blockAst.isReadOnly = true;
            }

        label_core:

            codes.Add(code);

            return this;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
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
}
