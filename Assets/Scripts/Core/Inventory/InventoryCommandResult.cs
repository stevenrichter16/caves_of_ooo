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

    public sealed class InventoryCommandResult
    {
        public bool Success { get; }

        public InventoryCommandErrorCode ErrorCode { get; }

        public string ErrorMessage { get; }

        public InventoryValidationResult Validation { get; }

        private InventoryCommandResult(
            bool success,
            InventoryCommandErrorCode errorCode,
            string errorMessage,
            InventoryValidationResult validation)
        {
            Success = success;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage ?? string.Empty;
            Validation = validation;
        }

        public static InventoryCommandResult Ok()
        {
            return new InventoryCommandResult(
                success: true,
                errorCode: InventoryCommandErrorCode.None,
                errorMessage: string.Empty,
                validation: InventoryValidationResult.Valid());
        }

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
            return new InventoryCommandResult(Success, ErrorCode, ErrorMessage, validation);
        }
    }
}
