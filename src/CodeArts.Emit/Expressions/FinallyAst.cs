using System.Diagnostics;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 结束。
    /// </summary>
    [DebuggerDisplay("finally\\{ {body} \\}")]
    public class FinallyAst : BlockAst
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public FinallyAst() : base(typeof(void))
        {
        }
    }
}
