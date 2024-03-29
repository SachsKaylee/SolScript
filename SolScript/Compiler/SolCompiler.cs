﻿using System.Collections.Generic;
using PSUtility.Enumerables;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Statements;
using SolScript.Utility;

namespace SolScript.Compiler
{
    /// <summary>
    ///     The compiler is used to compile an assembly into quicker to read binary data. The compiled data can then be read by
    ///     SolScript without the need for having to interpret it into a syntax tree first.
    /// </summary>
    public class SolCompiler
    {
        /// <summary>
        ///     Creates a new compiler instance.
        /// </summary>
        /// <param name="assembly">The assembly this compiler is for.</param>
        public SolCompiler(SolAssembly assembly)
        {
            Assembly = assembly;
        }

        /// <summary>
        ///     The assembly this compiler is for.
        /// </summary>
        public readonly SolAssembly Assembly;

        /// <summary>
        ///     Validates a class information to ensure that no duplicate functions or unimplemented abstract functions exist.
        /// </summary>
        /// <param name="definition">The class definition to validate.</param>
        /// <exception cref="SolCompilerException">The class definition is not valid.</exception>
        public void ValidateClass(SolClassDefinition definition)
        {
            Stack<SolClassDefinition> inheritanceChain = definition.GetInheritanceReversed();
            var funcNames = new PSDictionary<string, SolFunctionDefinition>();
            var abstracts = new PSDictionary<string, SolFunctionDefinition>();
            var fieldNames = new PSDictionary<string, SolFieldDefinition>();
            while (inheritanceChain.Count != 0) {
                SolClassDefinition current = inheritanceChain.Pop();
                var thisFuncNames = new PSHashSet<string>();
                var thisFieldNames = new PSHashSet<string>();
                foreach (SolFunctionDefinition function in current.DeclaredFunctions) {
                    ValidateFunctionHead(function);
                    // Every function name may only exist once for each inheritance level.
                    if (thisFuncNames.Contains(function.Name)) {
                        throw new SolCompilerException(function.Location, "The function \"" + FuncStr(function) + "\" exists twice within its class. Function overloading is currently not supported.");
                    }
                    thisFuncNames.Add(function.Name);
                    SolFieldDefinition field;
                    if (fieldNames.TryGetValue(function.Name, out field)) {
                        throw new SolCompilerException(function.Location, "The function \"" + FuncStr(function) + "\" conflicts with field \"" + FieldStr(field) + "\".");
                    }
                    // If another function with the same name already exists we can only
                    // continue if this one overrides the other one.
                    if (funcNames.ContainsKey(function.Name)) {
                        if (function.MemberModifier != SolMemberModifier.Override) {
                            throw new SolCompilerException(function.Location,
                                "The function \"" + FuncStr(function) + "\" hides a member declared at a lower level but does not have the override member modifier.");
                        }
                    }
                    // Ensure overriding is done right.
                    if (function.MemberModifier == SolMemberModifier.Abstract) {
                        abstracts.Add(function.Name, function);
                    } else if (function.MemberModifier == SolMemberModifier.Override) {
                        // A function with the override modifier must have a function in the same
                        // access context to actually override.
                        SolFunctionDefinition overrides;
                        if (!funcNames.TryGetValue(function.Name, out overrides)) {
                            throw new SolCompilerException(function.Location, "The function \"" + FuncStr(function) + "\" tried to override a function that does not exist.");
                        }
                        if (function.AccessModifier != overrides.AccessModifier) {
                            throw new SolCompilerException(function.Location, "The function \"" + FuncStr(function) + "\" tried to override a " + overrides.AccessModifier + " function, but was " +
                                                                              function.AccessModifier + " itself. Only functions with the same access modifier can override another.");
                        }
                        abstracts.Remove(function.Name);
                    }
                    // Local names are not relevant for the flat namespace.
                    // Setting the name at the end since we need to access the old function when checking override access.
                    if (function.AccessModifier != SolAccessModifier.Local) {
                        funcNames[function.Name] = function;
                    }
                }
                foreach (SolFieldDefinition field in current.DeclaredFields) {
                    if (thisFieldNames.Contains(field.Name)) {
                        throw new SolCompilerException(field.Location, "The field \"" + FieldStr(field) + " exists twice within its class.");
                    }
                    SolFieldDefinition duplField;
                    if (fieldNames.TryGetValue(field.Name, out duplField)) {
                        throw new SolCompilerException(field.Location, "The field \"" + FieldStr(field) + " has already been declared in class \"" + duplField.DefinedIn.NotNull().Type + "\".");
                    }
                    SolFunctionDefinition duplFunc;
                    if (funcNames.TryGetValue(field.Name, out duplFunc)) {
                        throw new SolCompilerException(field.Location, "The field \"" + FieldStr(field) + "\" conflicts with function \"" + FuncStr(duplFunc) + "\".");
                    }
                    if (thisFuncNames.Contains(field.Name)) {
                        throw new SolCompilerException(field.Location, "The field \"" + FieldStr(field) + "\" conflicts with a local function with the same name.");
                    }
                    thisFieldNames.Add(field.Name);
                    if (field.AccessModifier != SolAccessModifier.Local) {
                        fieldNames.Add(field.Name, field);
                    }
                }
            }
            // Non abstract class need to implement all abstract functions.
            if (definition.TypeMode != SolTypeMode.Abstract && abstracts.Count != 0) {
                throw new SolCompilerException(definition.Location, "The non-abstract class \"" + definition.Type + "\" has " + abstracts.Count +
                                                                    " unimplemented abstract function(s). Non-abstract classes need to implement all abstract functions. Function(s): " + abstracts.Values.JoinToString(", ", FuncStr));
            }
            ValidateMetaFunctions(definition);
        }

        /// <summary>
        ///     Validates the return type, parameter types, optional arguments and access modifier of the declared meta functions
        ///     in a class.
        /// </summary>
        /// <param name="definition">The class definition.</param>
        /// <exception cref="SolCompilerException">Invalid meta function.</exception>
        private void ValidateMetaFunctions(SolClassDefinition definition)
        {
            foreach (KeyValuePair<SolMetaFunction, SolClassDefinition.MetaFunctionLink> metaFunction in definition.DeclaredMetaFunctions) {
                SolFunctionDefinition functionDefinition = metaFunction.Value.Definition;
                SolMetaFunction meta = metaFunction.Key;
                if (functionDefinition.AccessModifier != SolAccessModifier.Internal) {
                    throw new SolCompilerException(functionDefinition.Location, $"The meta function \"{FuncStr(functionDefinition)}\" must be internal.");
                }
                if (!meta.Type.IsCompatible(Assembly, functionDefinition.Type)) {
                    throw new SolCompilerException(functionDefinition.Location,
                        $"The return type \"{functionDefinition.Type}\" of meta function \"{FuncStr(functionDefinition)}\" is not compatible with the required return type \"{meta.Type}\"");
                }
                if (meta.Parameters != null) {
                    if (functionDefinition.ParameterInfo.Count != meta.Parameters.Types.Count || functionDefinition.ParameterInfo.AllowOptional != meta.Parameters.AllowOptional) {
                        throw new SolCompilerException(functionDefinition.Location,
                            $"The meta function \"{FuncStr(functionDefinition)}\" has {functionDefinition.ParameterInfo.Count} parameters, and {(functionDefinition.ParameterInfo.AllowOptional ? "allows" : "does not allow")} optional arguments. The requirements for this function are {meta.Parameters.Types.Count} parameters and {(meta.Parameters.AllowOptional ? "allowed" : "not allowed")} optional parameters.");
                    }
                    for (int i = 0; i < meta.Parameters.Types.Count; i++) {
                        SolParameter parameter = functionDefinition.ParameterInfo[i];
                        if (!parameter.Type.IsCompatible(Assembly, meta.Parameters.Types[i])) {
                            throw new SolCompilerException(functionDefinition.Location,
                                $"The parameter \"{parameter.Name}\" of meta function \"{FuncStr(functionDefinition)}\" is of type \"{parameter.Type}\", but should be of type \"{meta.Parameters.Types[i]}\"(or a compatible one).");
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Validates a function. Keep in mind that even after calling this function some information may still be incorrect.
        ///     (e.g. this function does not check if an override function overrides an actual function. This check is not in the
        ///     class validation.)
        /// </summary>
        /// <param name="definition">The function definition.</param>
        /// <exception cref="SolCompilerException">The function definition is not valid.</exception>
        private void ValidateFunctionHead(SolFunctionDefinition definition)
        {
            // todo: expand this method to fully validate everything. relocate code from the class check. see summary for more detail.
            if (definition.AccessModifier == SolAccessModifier.Local) {
                // Local function
                if (definition.MemberModifier != SolMemberModifier.Default) {
                    throw new SolCompilerException(definition.Location,
                        "The function \"" + FuncStr(definition) + "\" has local access and thus cannot have the " + definition.MemberModifier + " member modifier.");
                }
            } else {
                // Internal / Public function
                if (definition.DefinedIn == null) {
                    // Global functions
                    if (definition.MemberModifier != SolMemberModifier.Default) {
                        throw new SolCompilerException(definition.Location,
                            "The function \"" + FuncStr(definition) + "\" is a global function and thus cannot have the " + definition.MemberModifier + " member modifier.");
                    }
                } else {
                    if (definition.MemberModifier == SolMemberModifier.Abstract && definition.DefinedIn.TypeMode != SolTypeMode.Abstract) {
                        throw new SolCompilerException(definition.Location, "The function \"" + FuncStr(definition) +
                                                                            "\" is abstract, but was declared in a non abstract class. Only abstract classes can contain abstract functions.");
                    }
                }
            }
        }

        /// <summary>
        /// Validates a cimplete chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        private void ValidateChunk(SolChunk chunk)
        {
            foreach (SolStatement statement in chunk.Statements) {
                
            }
        }



        /// <summary>
        ///     <see cref="SolClassDefinition.Type" />.<see cref="SolFunctionDefinition.Name" />
        /// </summary>
        private static string FuncStr(SolFunctionDefinition definition)
        {
            if (definition.DefinedIn != null) {
                return definition.DefinedIn.Type + "." + definition.Name;
            }
            return definition.Name;
        }

        /// <summary>
        ///     <see cref="SolClassDefinition.Type" />.<see cref="SolFieldDefinition.Name" />
        /// </summary>
        private static string FieldStr(SolFieldDefinition definition)
        {
            if (definition.DefinedIn != null) {
                return definition.DefinedIn.Type + "." + definition.Name;
            }
            return definition.Name;
        }
    }
}