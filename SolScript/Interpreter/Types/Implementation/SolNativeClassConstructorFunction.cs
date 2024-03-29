﻿using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This type represents a constructor imported from native code. Constructors
    ///     needs a different implementation since they don't have a backing
    ///     MethodInfo(instead a ConstructorInfo) and do not simply return a SolValue,
    ///     but instead create a NativeReference which needs to be registered inside the
    ///     SolClass to that a valid object for instance access exists.<br />If you are looking for the constructor function of
    ///     script functions: Script functions simply use a "normal" <see cref="SolScriptClassFunction" /> as constructor,
    ///     since the constructor is only a meta-function invoked upon creation of the class.
    /// </summary>
    /// <remarks>This is also the reason why native classes break if you do not invoke their constructor.</remarks>
    public sealed class SolNativeClassConstructorFunction : SolNativeClassFunction
    {
        /// <summary>
        ///     Creates a new constructor from the given parameters.
        /// </summary>
        /// <param name="instance">The class instance this function is the constructor of.</param>
        /// <param name="definition">The function definition of this constructor.</param>
        public SolNativeClassConstructorFunction([NotNull] IClassLevelLink instance, [NotNull] SolFunctionDefinition definition) : base(instance, definition) {}

        #region Overrides

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 12 + (int) Id;
            }
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured while calling the function.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured. Execution may have to be halted.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            if (DefinedIn.ClassInstance.IsInitialized) {
                throw new SolRuntimeException(context, "Cannot call constructor of an initialized \"" + DefinedIn + "\" class instance.");
            }
            object[] values;
            try {
                values = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Could not marshal the function parameters to native objects: " + ex.Message, ex);
            }
            SolClassDefinition classDefinition = Definition.DefinedIn.NotNull();
            // Why are we not directly using the descriptor of the class this ctor was defined in?
            // Rather simple. A class inheriting a native class will have a generated class be set
            // as its descriptor. This means that the required native descriptor will not be in the 
            // descriptor of the "actual" descriptor, but in the class of the overriding type.
            object descriptorObject = InternalHelper.SandboxInvokeMethod(context, GetMostSuitableNativeCtor(), null, values).NotNull();
            object describedObject;
            DefinedIn.ClassInstance.DescriptorObjectReference = new DynamicReference.FixedReference(descriptorObject);
            if (classDefinition.DescribedType != classDefinition.DescriptorType) {
                // todo: be able to specifcy ctor/factory in descriptor
                describedObject = Activator.CreateInstance(classDefinition.DescribedType);
                DefinedIn.ClassInstance.DescribedObjectReference = new DynamicReference.FixedReference(describedObject);
            } else {
                DefinedIn.ClassInstance.DescribedObjectReference = DefinedIn.ClassInstance.DescriptorObjectReference;
                describedObject = descriptorObject;
            }
            SolMarshal.GetAssemblyCache(Assembly).StoreReference(descriptorObject, DefinedIn.ClassInstance);
            // Assigning self after storing in assembly cache.
            SetSelf(describedObject as INativeClassSelf, DefinedIn.ClassInstance);
            if (!ReferenceEquals(descriptorObject, describedObject)) {
                SetSelf(descriptorObject as INativeClassSelf, DefinedIn.ClassInstance);
            }
            return SolNil.Instance;
        }

        #endregion

        private ConstructorInfo GetMostSuitableNativeCtor()
        {
            // todo: resolve dynmaically generated ctor statically? even possible/realistic?
            foreach (SolClassDefinition definition in DefinedIn.ClassInstance.Definition.GetInheritanceReversed()) {
                // todo: dynamic native class create ctor
                if (definition.DescriptorType != null 
                    && InternalHelper.IsClassSolCompilerGenerated(definition.DescriptorType))
                {
                    return definition.DescriptorType.GetConstructors().First();
                }
            }
            return Definition.Chunk.GetNativeConstructor();
        }

        private static void SetSelf(INativeClassSelf self, SolClass cls)
        {
            if (self != null) {
                if (self.Self != null) {
                    throw new SolMarshallingException("Type native Self value of native class \"" + self.GetType().Name + "\"(SolClass \"" + cls.Type
                                                      + "\") is not null. This is either an indicator for a duplicate native class or corrupted marshalling data.");
                }
                self.Self = cls;
            }
        }
    }
}