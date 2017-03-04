﻿using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This definitions contains information about a field in SolScript.
    /// </summary>
    public sealed class SolFieldDefinition : SolAnnotateableDefinitionBase
    {
        /// <summary>
        ///     Creates a new field definition for a field located in a class.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="definedIn">The class the field was defined in.</param>
        /// <param name="builder">The field builder.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for the native field type.</exception>
        public SolFieldDefinition(SolAssembly assembly, [CanBeNull] SolClassDefinition definedIn, SolFieldBuilder builder) : this(assembly, builder)
        {
            DefinedIn = definedIn;
        }

        /// <summary>
        ///     Creates a new field definition for a global field.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="builder">The field builder.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for the native field type.</exception>
        public SolFieldDefinition(SolAssembly assembly, SolFieldBuilder builder) : base(assembly, builder.Location)
        {
            Name = builder.Name;
            AccessModifier = builder.AccessModifier;
            MemberModifier = builder.MemberModifier;
            Type = builder.FieldType.Get(assembly);
            AnnotationsFromData(builder.Annotations);
            Initializer = builder.IsNative ? new SolFieldInitializerWrapper(builder.NativeField) : new SolFieldInitializerWrapper(builder.ScriptField);
        }

        /// <summary>
        ///     The field's access modifier.
        /// </summary>
        public readonly SolAccessModifier AccessModifier;

        /// <summary>
        ///     The class this field was defined in. This is null for global fields.
        /// </summary>
        [CanBeNull] public readonly SolClassDefinition DefinedIn;

        /// <summary>
        ///     This class wraps the initializer of the field. Make sure to check the
        ///     <see cref="SolFieldInitializerWrapper.FieldType" /> before obtaining an actual reference.
        /// </summary>
        public readonly SolFieldInitializerWrapper Initializer;

        /// <summary>
        ///     The member modifier specifying the type of field.
        /// </summary>
        /// <seealso cref="SolMemberModifier" />
        public readonly SolMemberModifier MemberModifier;

        /// <summary>
        ///     The name of the field.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The data type of the field.
        /// </summary>
        public readonly SolType Type;

        /// <inheritdoc />
        public override IReadOnlyList<SolAnnotationDefinition> Annotations { get; protected set; }
    }
}