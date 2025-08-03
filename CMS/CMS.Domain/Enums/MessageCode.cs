using System.ComponentModel;

public enum MessageCode
{
    [Description("The operation completed successfully.")]
    Success,

    [Description("No data found.")]
    NotFound,

    [Description("An error occurred during the operation.")]
    GeneralError,

    [Description("Invalid input parameters.")]
    InvalidInput,

    [Description("Failed to create the entity.")]
    CreateFailed,

    [Description("Failed to update the entity.")]
    UpdateFailed,

    [Description("Failed to delete the entity.")]
    DeleteFailed
}
