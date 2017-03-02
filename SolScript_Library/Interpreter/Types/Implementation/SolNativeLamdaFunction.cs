﻿using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This class represents native lamda function.
    /// </summary>
    /// <seealso cref="SolLamdaFunction" />
    public sealed class SolNativeLamdaFunction : SolLamdaFunction
    {
        /// <inheritdoc />
        private SolNativeLamdaFunction(SolAssembly assembly, SolParameterInfo parameterInfo, SolType returnType, MethodInfo method, DynamicReference instance)
            : base(assembly, SolSourceLocation.Native(), parameterInfo, returnType)
        {
            m_Method = method;
            m_Instance = instance;
        }

        // The reference to the object to invoke the method on.
        private readonly DynamicReference m_Instance;
        private readonly MethodInfo m_Method;

        /// <inheritdoc />
        public new SolParameterInfo.Native ParameterInfo => (SolParameterInfo.Native) base.ParameterInfo;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            DynamicReference.GetState getState;
            object target = m_Instance.GetReference(out getState);
            if (getState != DynamicReference.GetState.Retrieved) {
                throw new InvalidOperationException($"Failed to retieve native object reference for native lamda function \"{m_Method.Name}\".");
            }
            object[] nativeArgs;
            try {
                nativeArgs = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, $"Could not marshal arguments to native lamda function \"{m_Method.Name}\"", ex);
            }
            object nativeReturn = InternalHelper.SandboxInvokeMethod(context, m_Method, target, nativeArgs);
            SolValue solReturn;
            try {
                solReturn = SolMarshal.MarshalFromNative(Assembly, m_Method.ReturnType, nativeReturn);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, $"Could not marshal return value of type \"{nativeReturn?.GetType().Name ?? "null"}\" to SolScript.", ex);
            }
            return solReturn;
        }

        #endregion

        /// <summary>Creates a new native lamda functions for the given method.</summary>
        /// <param name="assembly">The assembly this function belongs to.</param>
        /// <param name="method">The native method representing this function.</param>
        /// <param name="instance">A reference to the object to invoke the method on.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for a parameter type.</exception>
        public static SolNativeLamdaFunction CreateFrom([NotNull] SolAssembly assembly, [NotNull] MethodInfo method, [NotNull] DynamicReference instance)
        {
            SolParameterBuilder[] parameters;
            Type[] marshalTypes;
            bool allowOptional;
            bool sendContext;
            InternalHelper.GetParameterBuilders(method.GetParameters(), out parameters, out marshalTypes, out allowOptional, out sendContext);
            SolType returnType = InternalHelper.GetMemberReturnType(assembly, method);
            SolParameterInfo parameterInfo = new SolParameterInfo.Native(parameters.Select(p => p.Get(assembly)).ToArray(), marshalTypes, allowOptional, sendContext);
            return new SolNativeLamdaFunction(assembly, parameterInfo, returnType, method, instance);
        }
    }
}