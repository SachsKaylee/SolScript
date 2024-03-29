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
using System.Text;
using JetBrains.Annotations;
using NodeParser;
using SolScript.Exceptions;

namespace SolScript
{
    /// <summary>
    ///     The <see cref="SolError" /> class is used for errors that occured either during the compliation or interpretation
    ///     of SolScript.
    /// </summary>
    public class SolError
    {
        /// <summary>
        ///     Creates a new error from the given parameters.
        /// </summary>
        /// <param name="location">The location this error occured at.</param>
        /// <param name="id"> The <see cref="ErrorId" /> which can be used for help lookup.</param>
        /// <param name="message"> The human readable error message.</param>
        /// <param name="isWarning">Is this error a warning?</param>
        /// <param name="exception"> The exception that caused this error. </param>
        public SolError(NodeLocation location, ErrorId id, string message, bool isWarning = false, [CanBeNull] Exception exception = null)
        {
            Location = location;
            Id = id;
            Message = message;
            IsWarning = isWarning;
            Exception = exception;
        }

        /// <summary>
        ///     Creates a new error from the given parameters.
        /// </summary>
        /// <param name="location">The location this error occured at.</param>
        /// <param name="message"> The human readable error message.</param>
        /// <param name="isWarning">Is this error a warning?</param>
        /// <param name="exception"> The exception that caused this error. </param>
        public SolError(NodeLocation location, string message, bool isWarning = false, [CanBeNull] Exception exception = null)
        {
            Location = location;
            Id = ErrorId.None;
            Message = message;
            IsWarning = isWarning;
            Exception = exception;
        }

        /// <summary>
        ///     The exception that caused this error. The exception may or may not be completely useless to you and is mainly meant
        ///     for internal debugging purposes.
        /// </summary>
        [CanBeNull]
        public Exception Exception { get; }

        /// <summary>
        ///     The <see cref="ErrorId" /> which can be used for help lookup.
        /// </summary>
        public ErrorId Id { get; }

        /// <summary>
        ///     Is this error a warning?
        /// </summary>
        /// <remarks>Keep in mind that depending on some settings even warnings might be treated as errors.</remarks>
        public bool IsWarning { get; }

        /// <summary>
        ///     The location this error occured at.
        /// </summary>
        public NodeLocation Location { get; }

        /// <summary>
        ///     The human readable error message.
        /// </summary>
        public string Message { get; }

        #region Overrides

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((SolError) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                int hashCode = Location.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Id;
                hashCode = (hashCode * 397) ^ (Message != null ? Message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsWarning.GetHashCode();
                hashCode = (hashCode * 397) ^ (Exception != null ? Exception.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(IsWarning ? "[WARNING#" : "[ERROR#");
            builder.Append((int) Id);
            builder.Append("] ");
            builder.Append(Location);
            builder.Append(" - ");
            builder.AppendLine(Message);
            if (Exception != null) {
                builder.Append("Caused by: ");
                builder.AppendLine(Exception.GetType().Name);
                SolException.UnwindExceptionStack(Exception, builder);
            }
            return builder.ToString();
        }

        #endregion

        /// <inheritdoc />
        protected bool Equals(SolError other)
        {
            return Location.Equals(other.Location) && Id == other.Id && string.Equals(Message, other.Message) && IsWarning == other.IsWarning && Equals(Exception, other.Exception);
        }
    }
}