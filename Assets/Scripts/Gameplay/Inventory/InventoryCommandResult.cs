using System;

namespace CavesOfOoo.Core.Inventory
{
    public enum InventoryCommandErrorCode
    {
        None = 0,
        ValidationFailed,
        ExecutionFailed,
        Exception
    }

    public enum InventoryCommandState { Cancelled, ReversibleFailure, Deferred, Completed, CommittedPartial }

    public sealed class InventoryCommandResult
    {
        public bool Success { get; }
        public InventoryCommandState State { get; }
        /// <summary>A world action crossed its irreversible boundary. Independent
        /// of ordinary inventory success; legacy callers still pay on partial throws.</summary>
        public bool ActionCommitted { get; }
        public string FailurePhase { get; }

        public InventoryCommandErrorCode ErrorCode { get; }

        public string ErrorMessage { get; }

        public InventoryValidationResult Validation { get; }

        private InventoryCommandResult(
            bool success,
            InventoryCommandErrorCode errorCode,
            string errorMessage,
            InventoryValidationResult validation,
            InventoryCommandState? state = null, bool actionCommitted = false, string failurePhase = "")
        {
            Success = success;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage ?? string.Empty;
            Validation = validation;
            State = state ?? (success ? InventoryCommandState.Completed : InventoryCommandState.ReversibleFailure);
            ActionCommitted = actionCommitted;
            FailurePhase = failurePhase ?? string.Empty;
        }

        public static InventoryCommandResult Ok(bool actionCommitted = false)
        {
            return new InventoryCommandResult(
                success: true,
                errorCode: InventoryCommandErrorCode.None,
                errorMessage: string.Empty,
                validation: InventoryValidationResult.Valid(), actionCommitted: actionCommitted);
        }

        public static InventoryCommandResult CommittedPartial(string message, string phase = "") =>
            new InventoryCommandResult(true, InventoryCommandErrorCode.ExecutionFailed, message,
                InventoryValidationResult.Valid(), InventoryCommandState.CommittedPartial, true, phase);

        public static InventoryCommandResult Deferred() => new InventoryCommandResult(false,
            InventoryCommandErrorCode.None, string.Empty, InventoryValidationResult.Valid(), InventoryCommandState.Deferred);

        public static InventoryCommandResult Cancelled(string message = "") => new InventoryCommandResult(false,
            InventoryCommandErrorCode.None, message, InventoryValidationResult.Valid(), InventoryCommandState.Cancelled);

        public InventoryCommandResult WithActionCommitted() => new InventoryCommandResult(Success,
            ErrorCode, ErrorMessage, Validation, State, true, FailurePhase);

        public static InventoryCommandResult Fail(
            InventoryCommandErrorCode errorCode,
            string errorMessage,
            InventoryValidationResult validation = null)
        {
            return new InventoryCommandResult(
                success: false,
                errorCode: errorCode,
                errorMessage: errorMessage,
                validation: validation);
        }

        public static InventoryCommandResult ValidationFailure(InventoryValidationResult validation)
        {
            if (validation == null || validation.IsValid)
            {
                validation = InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.Unknown,
                    "Validation failed.");
            }

            return new InventoryCommandResult(
                success: false,
                errorCode: InventoryCommandErrorCode.ValidationFailed,
                errorMessage: validation.ErrorMessage,
                validation: validation);
        }

        public static InventoryCommandResult FromException(Exception exception)
        {
            return new InventoryCommandResult(
                success: false,
                errorCode: InventoryCommandErrorCode.Exception,
                errorMessage: exception?.Message ?? "Unknown exception.",
                validation: null);
        }

        public InventoryCommandResult WithValidation(InventoryValidationResult validation)
        {
            return new InventoryCommandResult(Success, ErrorCode, ErrorMessage, validation, State, ActionCommitted, FailurePhase);
        }
    }
}
