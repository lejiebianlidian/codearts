using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 流程。
    /// </summary>
    /// <typeparam name="T">判断类型。</typeparam>
    public class SwitchAst<T> : AstExpression
    {
        private readonly AstExpression defaultAst;
        private readonly List<SwitchCase> switchCases = new List<SwitchCase>();
        private readonly AstExpression switchValue;

        private static readonly Type SwitchValueType = typeof(T);

        private class SwitchCase
        {
            public SwitchCase(T constantValue, AstExpression body)
            {
                ConstantValue = constantValue;
                Body = body;
            }

            public T ConstantValue { get; }
            public AstExpression Body { get; }
        }

        private SwitchAst(AstExpression switchValue, Type returnType) : base(returnType)
        {
            if (switchValue is null)
            {
                throw new ArgumentNullException(nameof(switchValue));
            }

            if (switchValue.RuntimeType == SwitchValueType || switchValue.RuntimeType == typeof(object) && SwitchValueType == typeof(Type))
            {
                this.switchValue = switchValue;
            }
            else
            {
                throw new ArgumentException($"“{switchValue.RuntimeType}”和“{SwitchValueType}”不能进行比较!");
            }
        }

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        public SwitchAst(AstExpression switchValue) : this(switchValue, typeof(void))
        {

        }

        /// <summary>
        /// 流程。
        /// </summary>
        public SwitchAst(AstExpression switchValue, AstExpression defaultAst) : this(switchValue, defaultAst.RuntimeType)
        {
            this.defaultAst = defaultAst ?? throw new ArgumentNullException(nameof(defaultAst));
        }

        /// <summary>
        /// 流程。
        /// </summary>
        public SwitchAst(AstExpression switchValue, AstExpression defaultAst, Type returnType) : this(switchValue, returnType)
        {
            if (defaultAst is null)
            {
                throw new ArgumentNullException(nameof(defaultAst));
            }

            if (EmitUtils.EqualSignatureTypes(defaultAst.RuntimeType, returnType) || defaultAst.RuntimeType.IsAssignableFrom(RuntimeType))
            {
                this.defaultAst = defaultAst;
            }
            else
            {
                throw new NotSupportedException($"默认模块“{defaultAst.RuntimeType}”和返回“{returnType}”类型无法默认转换!");
            }
        }

        /// <summary>
        /// 实例。
        /// </summary>
        /// <param name="constant">常量。</param>
        /// <param name="body">内容。</param>
        public void Case(T constant, AstExpression body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (EmitUtils.EqualSignatureTypes(defaultAst.RuntimeType, RuntimeType))
            {
                switchCases.Add(new SwitchCase(constant, body));
            }
            else if (body.RuntimeType.IsAssignableFrom(RuntimeType))
            {
                switchCases.Add(new SwitchCase(constant, new ConvertAst(body, RuntimeType)));
            }
            else
            {
                throw new NotSupportedException($"模块“{body.RuntimeType}”和返回“{RuntimeType}”类型无法默认转换!");
            }
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (switchCases.Count == 0)
            {
                defaultAst?.Load(ilg);

                return;
            }

            if (switchValue is VariableAst variableAst)
            {
            }
            else
            {
                LocalBuilder variable = ilg.DeclareLocal(switchValue.RuntimeType);

                switchValue.Load(ilg);

                ilg.Emit(OpCodes.Stloc, variable);

                var label = ilg.DefineLabel();

                if (IsArithmetic(RuntimeType))
                {
                    foreach (var switchCase in switchCases)
                    {
                        ilg.Emit(OpCodes.Ldloc, variable);

                        EmitUtils.EmitConstantOfType(ilg, switchCase.ConstantValue, SwitchValueType);
                    }
                }
                else
                {

                }

                ilg.MarkLabel(label);
            }
        }

        private static bool IsArithmetic(Type type)
        {
            if (type.IsEnum || type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
