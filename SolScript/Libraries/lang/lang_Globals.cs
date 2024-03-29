﻿using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.lang
{
    /// <summary>
    ///     All global functions and fields in the <see cref="lang" /> library.
    /// </summary>
    [PublicAPI, SolGlobalTypeDescriptor(lang.NAME)]
    public static class lang_Globals
    {
        private const string DEFAULT_ERROR_MESSAGE = "An error occured.";
        private const string DEFAULT_ASSERTION_FAILED_MESSAGE = "Assertion failed!";

        /// <summary>
        ///     Raises an execution error with the given message.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message">The error message. (Default: "An error occured.")</param>
        /// <exception cref="SolRuntimeException">
        ///     Throws a <see cref="SolRuntimeException" /> with the value of
        ///     <paramref name="message" /> as <see cref="Exception.Message" />.
        /// </exception>
        [SolVisibility(true)]
        public static void error(SolExecutionContext context, [SolContract(SolString.TYPE, true), CanBeNull] SolString message)
        {
            string messageStr = message?.Value ?? DEFAULT_ERROR_MESSAGE;
            throw new SolRuntimeException(context, messageStr);
        }

        /// <summary>
        ///     Asserts that a certain expression is not false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The value to assert.</param>
        /// <param name="message">The error message if the assertion fails. (Default: "Assertion failed!")</param>
        /// <remarks>
        ///     Be wary that this message asserts that an expression is NOT FALSE. <br />
        ///     A few examples:
        ///     <br />
        ///     <c>
        ///         assert(false) -> ERROR!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert(nil) -> ERROR!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert(true) -> OK!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert(0) -> OK!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert({}) -> OK!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert("") -> OK!
        ///     </c>
        /// </remarks>
        /// <exception cref="SolRuntimeException">The assertion failed.</exception>
        [SolVisibility( true)]
        public static void assert(SolExecutionContext context, SolValue value, [SolContract(SolString.TYPE, true), CanBeNull] SolString message)
        {
            if (value.IsFalse(context)) {
                throw new SolRuntimeException(context, message?.Value ?? DEFAULT_ASSERTION_FAILED_MESSAGE);
            }
        }

        /// <summary>
        ///     Prints values to the standard output.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="values">The values to print.</param>
        /// <exception cref="SolRuntimeException">The <see cref="SolAssembly.OutputEncoding" /> is not supported on this system.</exception>
        /// <exception cref="SolRuntimeException">An I/O error occured while writing to the standard output.</exception>
        /// <exception cref="SolRuntimeException">The standard output does not support writing.</exception>
        /// <exception cref="SolRuntimeException">The standard output has been closed.</exception>
        [SolVisibility( true)]
        public static void print(SolExecutionContext context, params SolValue[] values)
        {
            StringBuilder builder = new StringBuilder();
            SolStackFrame frame;
            // We are peeking at a depth of one since zero would be this
            // method. And the user doesn't need to know that the print
            // method is the print method.
            if (context.PeekStackFrame(out frame, 1)) {
                frame.AppendFunctionName(builder);
            } else {
                builder.Append(SolSourceLocation.NATIVE_FILE);
            }
            builder.Append(" [");
            builder.Append(context.CurrentLocation);
            builder.Append("] : ");
            builder.Append(values.JoinToString(", ", value => value.ToString(context)));
            builder.Append(Environment.NewLine);
            string str = builder.ToString();
            byte[] bytes;
            try {
                bytes = context.Assembly.OutputEncoding.GetBytes(str.ToCharArray(), 0, str.Length);
            } catch (EncoderFallbackException ex) {
                throw new SolRuntimeException(context, "The assembly output encoding \"" + context.Assembly.OutputEncoding.EncodingName + "\" is not supported on this system.", ex);
            }
            try {
                context.Assembly.Output.Write(bytes, 0, bytes.Length);
            } catch (IOException ex) {
                throw new SolRuntimeException(context, "An I/O error occured while writing to the standard output.", ex);
            } catch (NotSupportedException ex) {
                throw new SolRuntimeException(context, "The standard output does not support writing. Huston, we may have a problem.", ex);
            } catch (ObjectDisposedException ex) {
                throw new SolRuntimeException(context, "The standard output has been closed.", ex);
            }
        }

        /// <summary>
        ///     Gets the type of the given <paramref name="value" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The type.</returns>
        [SolVisibility( true)]
        [SolContract(SolString.TYPE, false)]
        public static SolString type([SolContract(SolValue.ANY_TYPE, true)] SolValue value)
        {
            return SolString.ValueOf(value.Type);
        }

        /// <summary>
        ///     Converts a value to its string representation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The string representation of the given <paramref name="value" />.</returns>
        [SolVisibility( true)]
        [SolContract(SolString.TYPE, false)]
        public static SolString to_string(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue value)
        {
            return value.ToString(context);
        }

        /// <summary>
        ///     Checks if the two given values are considered equal.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value1">The first value. If this value is a class its equality meta function will be called.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>true if both values can be considered equal, false if not.</returns>
        [SolVisibility( true)]
        [SolContract(SolBool.TYPE, false)]
        public static SolBool equals(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue value1, [SolContract(SolValue.ANY_TYPE, true)] SolValue value2)
        {
            return SolBool.ValueOf(value1.IsEqual(context, value2));
        }

        /// <summary>
        ///     Checks if the two given values have the same reference.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>true if both values share the same reference.</returns>
        /// <remarks>
        ///     Primitive types are still checked for equality since they are immutable. And there is no actual use case in
        ///     which e.g. a number should not be equal to itself.
        /// </remarks>
        [SolVisibility( true)]
        [SolContract(SolBool.TYPE, false)]
        public static SolBool reference_equals(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue value1, [SolContract(SolValue.ANY_TYPE, true)] SolValue value2)
        {
            return SolBool.ValueOf(value1.IsReferenceEqual(context, value2));
        }
    }
}