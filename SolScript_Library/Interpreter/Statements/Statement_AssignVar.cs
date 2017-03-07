﻿using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     The assign var statement is used to assign values to variables. It can be chained due to also being an expression.
    /// </summary>
    public class Statement_AssignVar : SolStatement
    {
        /// <summary>
        ///     Creates a new statement.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The location in code.</param>
        /// <param name="target">The operation used to actually assign the value.</param>
        /// <param name="valueGetter">The expression used to obtain the value that should be assigned.</param>
        public Statement_AssignVar(SolAssembly assembly, SolSourceLocation location, TargetRef target, SolExpression valueGetter) : base(assembly, location)
        {
            Target = target;
            ValueGetter = valueGetter;
        }

        /// <summary>
        ///     The operation used to actually assign the value.
        /// </summary>
        public readonly TargetRef Target;

        /// <summary>
        ///     The expression used to obtain the value that should be assigned.
        /// </summary>
        public readonly SolExpression ValueGetter;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">Failed to assign the variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            SolValue value = ValueGetter.Evaluate(context, parentVariables);
            try {
                value = Target.Set(value, context, parentVariables);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to assign the variable.", ex);
            }
            terminators = Terminators.None;
            return value;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return $"{Target} = {ValueGetter}";
        }

        #endregion

        #region Nested type: IndexedVariable

        /// <summary>
        ///     An indexed variable has an <see cref="IndexableGetter" /> and a <see cref="KeyGetter" />. The indexable getter must
        ///     return an <see cref="IValueIndexable" /> which will then be indexed by the result of the <see cref="KeyGetter" />.
        /// </summary>
        public class IndexedVariable : TargetRef
        {
            /// <summary>
            ///     Creates a new indexed variable.
            /// </summary>
            /// <param name="indexableGetter">The expression used to get the value that should be indexed.</param>
            /// <param name="keyGetter">The expression used to get the key the indexed value should be indexed by.</param>
            public IndexedVariable(SolExpression indexableGetter, SolExpression keyGetter)
            {
                IndexableGetter = indexableGetter;
                KeyGetter = keyGetter;
            }

            /// <summary>
            ///     The value that will be indexed. The return value must implement <see cref="IValueIndexable" />.
            /// </summary>
            public readonly SolExpression IndexableGetter;

            /// <summary>
            ///     The key by which the result of <see cref="IndexableGetter" /> will be indexed.
            /// </summary>
            public readonly SolExpression KeyGetter;

            #region Overrides

            /// <inheritdoc />
            /// <remarks> Evaluates the <see cref="IndexableGetter" /> first, then the <see cref="KeyGetter" /> </remarks>
            /// <exception cref="SolVariableException">An error occured.</exception>
            public override SolValue Set(SolValue value, SolExecutionContext context, IVariables parentVariables)
            {
                SolValue indexableRaw = IndexableGetter.Evaluate(context, parentVariables);
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable == null) {
                    throw new SolVariableException("Cannot index the type \"" + indexableRaw.Type + "\".");
                }
                return indexable[KeyGetter.Evaluate(context, parentVariables)] = value;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return $"{IndexableGetter}[{KeyGetter}]";
            }

            #endregion
        }

        #endregion

        #region Nested type: NamedVariable

        /// <summary>
        ///     The <see cref="NamedVariable" /> will simply assign the value to the variable named <see cref="Name" />.
        /// </summary>
        public class NamedVariable : TargetRef
        {
            /// <summary>
            ///     Creates a new named variable.
            /// </summary>
            /// <param name="name">The name of the variable.</param>
            public NamedVariable(string name)
            {
                Name = name;
            }

            /// <summary>
            ///     The name of the variable the value will be assinged to.
            /// </summary>
            public readonly string Name;

            #region Overrides

            /// <inheritdoc />
            /// <exception cref="SolVariableException">An error occured.</exception>
            public override SolValue Set(SolValue value, SolExecutionContext context, IVariables parentVariables)
            {
                return parentVariables.Assign(Name, value);
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return Name;
            }

            #endregion
        }

        #endregion

        #region Nested type: TargetRef

        /// <summary>
        ///     The TargetRef class is used to provide an abstract way to set variables in a variable assignment statement.
        /// </summary>
        public abstract class TargetRef
        {
            /// <summary>
            ///     Sets the variable to the given value.
            /// </summary>
            /// <param name="value">The value to set the variable to.</param>
            /// <param name="context">The current context.</param>
            /// <param name="parentVariables">The parent variable context.</param>
            /// <exception cref="SolVariableException">An error occured.</exception>
            public abstract SolValue Set(SolValue value, SolExecutionContext context, IVariables parentVariables);
        }

        #endregion
    }
}