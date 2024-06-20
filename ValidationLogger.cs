/*
CodeBit Metadata

&name=Bredd.tech/ValidationLogger.cs
&description="Logger that accumulates messages during item validation and formats them for reporting."
&author="Brandt Redd"
&url=https://raw.githubusercontent.com/bredd/ValidationLogger/main/ValidationLogger.cs
&version=1.0.0-beta1
&keywords=CodeBit
&datePublished=2024-06-19
&license=https://opensource.org/licenses/BSD-3-Clause

About Codebits: http://codebit.net
*/


using System;
using System.Collections.Generic;
using System.Text;

namespace Bredd.CodeBit {

    [Flags]
    enum ValidationLevel {
        /// <summary>
        /// No messages logged
        /// </summary>
        None = 0,

        /// <summary>
        /// Used for verbose tracing of a validation system
        /// </summary>
        Trace = 1,

        /// <summary>
        /// Message related to debugging the validation system itself
        /// </summary>
        Debug = 2,

        /// <summary>
        /// Information about the element being validated that is not related
        /// to its actual validity
        /// </summary>
        Information = 4,

        /// <summary>
        /// There is a problem with the element being validated that is either
        /// tolerable or that the validator has corrected unambiguously.
        /// </summary>
        Warning = 8,

        /// <summary>
        /// The item failed validation.
        /// </summary>
        Error = 16,

        /// <summary>
        /// All flags
        /// </summary>
        All = 31
    }

    interface IValidationLogger {
        IDisposable BeginScope(string scopeName);

        void Log(ValidationLevel validationLevel, string propertyName, string message);

        ValidationLevel EnabledLevels { get; }
        bool IsEnabled(ValidationLevel level);
    }

    internal class ValidationLogger : IValidationLogger
    {
        const ValidationLevel c_defaultLevels = (ValidationLevel.Information | ValidationLevel.Warning | ValidationLevel.Error); 
        readonly List<LogMessage> m_log = new List<LogMessage>();
        readonly List<string> m_Scope = new List<string>();
        ValidationLevel m_enabledLevels;
        ValidationLevel m_loggedLevels;
        int m_errors = 0;
        int m_warnings = 0;

        public ValidationLogger(ValidationLevel enabledLevels = c_defaultLevels) {
            m_enabledLevels = enabledLevels;
        }

        public IDisposable BeginScope(string scopeName) {
            m_Scope.Add(scopeName);
            return new ScopeContext(this, m_Scope.Count);
        }

        private void EndScope(int level) {
            if (level > 0 && m_Scope.Count >= level) {
                --level;
                m_Scope.RemoveRange(level, m_Scope.Count - level);
            }
        }

        public void Log(ValidationLevel validationLevel, string propertyName, string message) {
            switch (validationLevel) {
            case ValidationLevel.Trace:
            case ValidationLevel.Debug:
            case ValidationLevel.Information:
                break; // Acceptable value, proceed
            case ValidationLevel.Warning:
                ++m_warnings;
                break;
            case ValidationLevel.Error:
                ++m_errors;
                break;
            default:
                throw new ArgumentException("Must be Trace, Debug, Information, Warning, or Error", nameof(validationLevel));
            }

            if ((validationLevel & m_enabledLevels) == 0) return;
            m_loggedLevels |= validationLevel;

            m_log.Add(new LogMessage(m_Scope.ToArray(), validationLevel, propertyName, message));
        }

        public ValidationLevel EnabledLevels { get => m_enabledLevels; set => m_enabledLevels = value; }

        public bool IsEnabled(ValidationLevel validationLevel) {
            return (validationLevel & m_enabledLevels) != 0;
        }

        public ValidationLevel LoggedLevels => m_loggedLevels;

        public int Errors => m_errors;

        public int Warnings => m_warnings;

        public bool PassedValidation => (m_loggedLevels & ValidationLevel.Error) == 0;

        public bool HasWarning => (m_loggedLevels & ValidationLevel.Warning) != 0;

        public bool HasFlag(ValidationLevel validationLevel) {
            return (m_loggedLevels & validationLevel) != 0;
        }

        public IReadOnlyList<LogMessage> LogMessages => m_log;

        public override string ToString() {
            var sb = new StringBuilder();
            var scope = new string[0];
            foreach (var message in m_log) {
                // Find extent of scope match
                int ctxMatch;
                for (ctxMatch = 0; ctxMatch < Math.Min(scope.Length, message.Scope.Length); ++ctxMatch) {
                    if (string.CompareOrdinal(scope[ctxMatch], message.Scope[ctxMatch]) != 0)
                        break;
                }
                
                // Clear closed scopes
                for (int i=scope.Length; i>ctxMatch; --i) {
                    sb.Append(' ', (i - 1) * 2);
                    sb.AppendLine("}");
                }
                scope = message.Scope;
                
                // Open new scopes
                for (int i=ctxMatch; i<scope.Length; ++i) {
                    sb.Append(' ', i * 2);
                    sb.Append(scope[i]);
                    sb.AppendLine(" {");
                }

                // Write the message
                sb.Append(' ', scope.Length * 2);
                sb.Append(message.ValidationLevel);
                sb.Append(": ");
                sb.Append(message.PropertyName);
                sb.Append(": ");
                sb.AppendLine(message.Message);
            }
            // Clear scope
            for (int i=scope.Length; i>0; --i) {
                sb.Append(' ', (i - 1) * 2);
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        public class LogMessage {
            public LogMessage(string[] scope, ValidationLevel validationLevel, string propertyName, string message) {
                Scope = scope;
                ValidationLevel = validationLevel;
                PropertyName = propertyName;
                Message = message;
            }

            public string[] Scope { get; private set; }
            public ValidationLevel ValidationLevel { get; private set; }
            public string PropertyName { get; private set; }
            public string Message { get; private set; }
        }

        class ScopeContext : IDisposable {
            ValidationLogger m_logger;
            int m_level;
            
            public ScopeContext(ValidationLogger logger, int level) {
                m_logger = logger;
                m_level = level;
            }

            public void Dispose() {
                if (m_level <= 0) return; // Already disposed
                m_logger.EndScope(m_level);
                m_level = 0;
            }
        } 
    }
}
