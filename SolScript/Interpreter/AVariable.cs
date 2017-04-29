﻿using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Interfaces;
using SolScript.Properties;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Base class for a variable. The variable is used to dynamically resolve values at runtime.
    /// </summary>
    public abstract class AVariable
    {
        /// <summary>
        ///     Gets the value.
        /// </summary>
        /// <param name="context">The context to use.</param>
        /// <param name="parentVariables">The parented variables.</param>
        /// <returns>The value.</returns>
        /// <exception cref="SolVariableException">
        ///     An error occured while retrieving this value. All other possible exceptions are
        ///     wrapped inside this exception.
        /// </exception>
        public abstract SolValue Get(SolExecutionContext context, IVariables parentVariables);

        /// <summary>
        ///     Sets the variable to the given value.
        /// </summary>
        /// <param name="value">The value to set the variable to.</param>
        /// <param name="context">The current context.</param>
        /// <param name="parentVariables">The parent variable context.</param>
        /// <exception cref="SolVariableException">
        ///     An error occured while setting this value. All other possible exceptions are
        ///     wrapped inside this exception.
        /// </exception>
        public abstract SolValue Set(SolValue value, SolExecutionContext context, IVariables parentVariables);

        #region Nested type: Indexed

        /// <summary>
        ///     Gets a variable by two indexing expressions.
        /// </summary>
        public class Indexed : AVariable
        {
            /// <summary>
            ///     Used by the parser.
            /// </summary>
            [Obsolete("Used by the parser.", true)]
            public Indexed() {}

            /// <summary>
            ///     Creates a new indexed variable.
            /// </summary>
            /// <param name="indexableGetter">The value which will be indexed(e.g. a table or class).</param>
            /// <param name="keyGetter">The value which will be used as a key during indexing.</param>
            public Indexed(SolExpression indexableGetter, SolExpression keyGetter)
            {
                IndexableGetter = indexableGetter;
                KeyGetter = keyGetter;
            }

            /// <summary>
            ///     The value which will be indexed(e.g. a table or class) - Resolved first.
            /// </summary>
            public SolExpression IndexableGetter { get; [UsedImplicitly] internal set; }

            /// <summary>
            ///     The value which will be used as a key during indexing. - Resolved second. Not resolved if the first failed.
            /// </summary>
            public SolExpression KeyGetter { get; [UsedImplicitly] internal set; }

            #region Overrides

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     An error occured while retrieving this value. All other possible exceptions are
            ///     wrapped inside this exception.
            /// </exception>
            public override SolValue Get(SolExecutionContext context, IVariables parentVariables)
            {
                SolValue indexableRaw = IndexableGetter.Evaluate(context, parentVariables);
                SolValue key = KeyGetter.Evaluate(context, parentVariables);
                SolClass solClass = indexableRaw as SolClass;
                if (solClass != null) {
                    SolString keyString = key as SolString;
                    if (keyString == null) {
                        throw new SolVariableException(KeyGetter.Location, Resources.Err_InvalidIndexerType.ToString(solClass.Type, key.Type));
                    }
                    // 1 Inheritance could be found -> We can access locals! An inheritance can be found if the
                    //   get expression was declared inside the class.
                    // 2 Not found -> Only global access.
                    // Kind of funny how this little null coalescing operator handles the "deciding part" of access rights.
                    // todo: Inheritance; the inheritance of the current class may not be found, but one the current class inherited from. 
                    SolClass.Inheritance inheritance = context.CurrentClass != null ? solClass.FindInheritance(context.CurrentClass.InheritanceChain.Definition) : null;
                    SolValue value = inheritance?.GetVariables(SolAccessModifier.Local, SolVariableMode.All).Get(keyString.Value)
                                     ?? solClass.InheritanceChain.GetVariables(SolAccessModifier.Global, SolVariableMode.All).Get(keyString.Value);
                    return value;
                }
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable != null) {
                    SolValue value = indexable[key];
                    return value;
                }
                throw new SolVariableException(IndexableGetter.Location, Resources.Err_InvalidIndexType.ToString(indexableRaw.Type));
            }

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     An error occured while setting this value. All other possible exceptions are
            ///     wrapped inside this exception.
            /// </exception>
            public override SolValue Set(SolValue value, SolExecutionContext context, IVariables parentVariables)
            {
                SolValue indexableRaw = IndexableGetter.Evaluate(context, parentVariables);
                SolValue key = KeyGetter.Evaluate(context, parentVariables);
                SolClass solClass = indexableRaw as SolClass;
                if (solClass != null) {
                    SolString keyString = key as SolString;
                    if (keyString == null) {
                        throw new SolVariableException(KeyGetter.Location, $"Tried to index a class with a \"{key.Type}\" value.");
                    }
                    // 1 Inheritance could be found -> We can access locals! An inheritance can be found if the
                    //   get expression was declared inside the class.
                    // 2 Not found -> Only global access.
                    // Kind of funny how this little null coalescing operator handles the "deciding part" of access rights.
                    // todo: Inheritance; the inheritance of the current class may not be found, but one the current class inherited from. 
                    SolClass.Inheritance inheritance = context.CurrentClass != null ? solClass.FindInheritance(context.CurrentClass.InheritanceChain.Definition) : null;
                    value = inheritance?.GetVariables(SolAccessModifier.Local, SolVariableMode.All).Assign(keyString.Value, value)
                            ?? solClass.InheritanceChain.GetVariables(SolAccessModifier.Global, SolVariableMode.All).Assign(keyString.Value, value);
                    return value;
                }
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable != null) {
                    value = indexable[key] = value;
                    return value;
                }
                throw new SolVariableException(IndexableGetter.Location, "Tried to index a \"" + indexableRaw.Type + "\" value.");
            }

            #endregion
        }

        #endregion

        #region Nested type: Named

        /// <summary>
        ///     Gets a variable by its name.
        /// </summary>
        public class Named : AVariable
        {
            /// <summary>
            ///     Creates a new named variable.
            /// </summary>
            /// <param name="name">The name of the variable.</param>
            public Named(string name)
            {
                Name = name;
            }

            /// <summary>
            ///     The name of the variable.
            /// </summary>
            public string Name { get; }

            #region Overrides

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     An error occured while retrieving this value. All other possible exceptions are
            ///     wrapped inside this exception.
            /// </exception>
            public override SolValue Get(SolExecutionContext context, IVariables parentVariables)
            {
                return parentVariables.Get(Name);
            }

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     An error occured while setting this value. All other possible exceptions are
            ///     wrapped inside this exception.
            /// </exception>
            public override SolValue Set(SolValue value, SolExecutionContext context, IVariables parentVariables)
            {
                return parentVariables.Assign(Name, value);
            }

            #endregion
        }

        #endregion
    }
}