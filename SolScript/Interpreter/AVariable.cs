﻿// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Official repository: https://bitbucket.org/PatrickSachs/solscript/
// ---------------------------------------------------------------------
// Copyright 2017 Patrick Sachs
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System;
using System.Linq;
using PSUtility.Strings;
using SolScript.Compiler;
using SolScript.Exceptions;
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
    public abstract class AVariable : ISolCompileable
    {
        #region ISolCompileable Members

        /// <inheritdoc />
        public abstract ValidationResult Validate(SolValidationContext context);

        #endregion

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
            public SolExpression IndexableGetter { get; }

            /// <summary>
            ///     The value which will be used as a key during indexing. - Resolved second. Not resolved if the first failed.
            /// </summary>
            public SolExpression KeyGetter { get; }

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
                    // (??) what does this means? -> todo: Inheritance; the inheritance of the current class may not be found, but one the current class inherited from. 
                    SolClassEntry entry;
                    // Q: We probably aren't always allowed to access locals if we find an inheritance. Maybe only if the
                    // definitions match?
                    // A: No, this is correct. If we find the inheritance of the level we are currently in we can only access
                    // the locals of the level we are in, not all locals or the "top-level" locals.
                    SolClass.Inheritance inheritance;
                    if (!context.PeekClassEntry(out entry) || entry.IsGlobal) {
                        inheritance = null;
                    } else {
                        inheritance = solClass.FindInheritance(entry.Level);
                    }
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
                    SolClassEntry entry;
                    SolClass.Inheritance inheritance;
                    if (!context.PeekClassEntry(out entry) || entry.IsGlobal) {
                        inheritance = null;
                    } else {
                        inheritance = solClass.FindInheritance(entry.Level);
                    }
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

            /// <inheritdoc />
            /// <exception cref="SolCompilerException">Internal state corruption.</exception>
            public override ValidationResult Validate(SolValidationContext context)
            {
                ValidationResult indexable = IndexableGetter.Validate(context);
                ValidationResult key = KeyGetter.Validate(context);
                // We can continue even if they key failed.
                if (!indexable) {
                    return ValidationResult.Failure();
                }
                SolType dataType;
                bool success = indexable && key;
                Type type = SolMarshal.GetNativeSolType(IndexableGetter.Assembly, indexable.Type.Type, SolMarshal.NativeClassRepresentation.SolClass);
                // We have special handling for classes.
                // They need to check access rights, etc.
                if (typeof(SolClass).IsAssignableFrom(type)) {
                    SolClassDefinition definition;
                    if (!IndexableGetter.Assembly.TryGetClass(indexable.Type.Type, out definition)) {
                        throw new SolCompilerException(IndexableGetter.Location, "Critical internal state corruption. The class " + indexable.Type.Type
                                                                                 + " could be resolved ealier but failed to resolve now. Did another thread manipulate the class definitons?");
                    }
                    // Any types other than strings use the dynmaic indexer.
                    SolString constant = KeyGetter.IsConstant
                        ? KeyGetter.Evaluate(null, null) as SolString
                        : null;
                    if (constant != null) {
                        string constantStr = constant.Value;
                        SolMemberDefinition member;
                        // todo: dynmaic index strings -- maybe class index no longer over strings?! -- make it truly statically typed.
                        if (!definition.TryGetMember(constant, false, out member)) {
                            context.Errors.Add(new SolError(KeyGetter.Location, CompilerResources.Err_ClassFieldMissing.FormatWith(definition.Type, constantStr)));
                            success = false;
                            dataType = default(SolType);
                        } else {
                            dataType = member.Type;
                            // Time to check if we are even allowed to access the field.
                            SolClassDefinition inDefinition = context.InClassDefinition;
                            if (inDefinition == null) {
                                // If we are in global context the field must be global aswell.
                                if (member.AccessModifier != SolAccessModifier.Global) {
                                    context.Errors.Add(new SolError(KeyGetter.Location, CompilerResources.Err_ClassFieldNotVisibleToGlobal.FormatWith(definition.Type, member)));
                                    success = false;
                                }
                            } else {
                                // If we are in a class:
                                //   ... if we are in the same class we are good to go.
                                //   ... if we are in the same inheritance the field must not be local.
                                //   ... if we are in a different class entirely the field must be global.
                                if (inDefinition.Extends(definition)) {
                                    // Watch the order of the extends check above.
                                    // If the class we are in extends the other one we can access the internals.
                                    // This ensures that we cannot access members that are not declared yet. At the
                                    // same time it allows us to access members that will be overridden.
                                    //  ... same inheritcane but not self
                                    if (member.AccessModifier == SolAccessModifier.Local) {
                                        context.Errors.Add(new SolError(KeyGetter.Location, CompilerResources.Err_ClassFieldNotVisibleToClass.FormatWith(definition.Type, member, inDefinition.Type)));
                                        success = false;
                                    }
                                } else if (definition != inDefinition) {
                                    //  ... other class
                                    if (member.AccessModifier != SolAccessModifier.Global) {
                                        context.Errors.Add(new SolError(KeyGetter.Location, CompilerResources.Err_ClassFieldNotVisibleToClass.FormatWith(definition.Type, member, inDefinition.Type)));
                                        success = false;
                                    }
                                }
                            }
                        }
                    } else {
                        SolDebug.WriteLine("Dynamic Class Indexer");
                        dataType = SolType.AnyNil;
                        // todo: indexing classes by non string values is allowed by the validator for now. This still requires runtime implementation though!
                        // once implemented possibly check if definition has dynamic indexer defined?
                    }
                } else if (type.GetInterfaces().Any(i => i == typeof(IValueIndexable))) {
                    // Non classes check if they are indexable. If they are we are cool.
                    dataType = SolType.AnyNil;
                } else {
                    context.Errors.Add(new SolError(IndexableGetter.Location, CompilerResources.Err_CannotIndexType.FormatWith(indexable.Type)));
                    success = false;
                    dataType = default(SolType);
                }
                return new ValidationResult(success, dataType);
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return IndexableGetter + "[" + KeyGetter + "]";
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

            /// <inheritdoc />
            public override ValidationResult Validate(SolValidationContext context)
            {
                SolType type;
                if (context.TryGetChunkVariable(Name, out type)) {
                    return new ValidationResult(true, type);
                }
                if (context.InClassDefinition != null) {
                    SolFieldDefinition field;
                    if (context.InClassDefinition.TryGetField(Name, false, out field)) {
                        return new ValidationResult(true, field.Type);
                    }
                }
                return ValidationResult.Failure();
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return Name;
            }

            #endregion
        }

        #endregion
    }
}