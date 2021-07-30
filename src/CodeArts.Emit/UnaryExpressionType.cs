namespace CodeArts.Emit
{
    /// <summary>
    /// 一元运算符。
    /// </summary>
    public enum UnaryExpressionType
    {
        /// <summary>
        /// A node that represents a unary plus operation. The result of a predefined unary plus operation is simply the value of the operand, but user-defined implementations may have non-trivial results.
        /// </summary>
        UnaryPlus,
        /// <summary>
        /// An arithmetic negation operation, such as (-a). The object a should not be modified in place.
        /// </summary>
        Negate,
        /// <summary>
        /// A bitwise complement or logical negation operation. In C#, it is equivalent to (~a) for integral types and to (!a) for Boolean values. In Visual Basic, it is equivalent to (Not a). The object a should not be modified in place.
        /// </summary>
        Not,
        /// <summary>
        /// A unary increment operation, such as (a + 1) in C# and Visual Basic. The object a should not be modified in place.
        /// </summary>
        Increment,
        /// <summary>
        /// A unary decrement operation, such as (a - 1) in C# and Visual Basic. The object a should not be modified in place.
        /// </summary>
        Decrement,
        /// <summary>
        /// A false condition value.
        /// </summary>
        IsFalse
    }
}
