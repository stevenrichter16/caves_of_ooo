namespace CavesOfOoo.Core.Inventory
{
    public enum InventoryValidationErrorCode
    {
        None = 0,
        InvalidActor,
        InvalidItem,
        InvalidZone,
        MissingInventoryPart,
        MissingBodyPart,
        MissingEquippablePart,
        NotTakeable,
        NotOwned,
        NoCompatibleSlot,
        OverWeightLimit,
        BlockedByRule,
        Unknown
    }

    public sealed class InventoryValidationResult
    {
        public bool IsValid { get; }

        public InventoryValidationErrorCode ErrorCode { get; }

        public string ErrorMessage { get; }

        private InventoryValidationResult(
            bool isValid,
            InventoryValidationErrorCode errorCode,
            string errorMessage)
        {
            IsValid = isValid;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public static InventoryValidationResult Valid()
        {
            return new InventoryValidationResult(true, InventoryValidationErrorCode.None, string.Empty);
        }

        public static InventoryValidationResult Invalid(
            InventoryValidationErrorCode errorCode,
            string errorMessage)
        {
            return new InventoryValidationResult(false, errorCode, errorMessage);
        }
    }
}
