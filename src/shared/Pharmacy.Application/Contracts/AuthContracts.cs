namespace Pharmacy.Application.Contracts;

public sealed record LoginRequest(string UserName, string Password, string DeviceId);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, string UserName, string FullName,
    string Role);

public sealed record UserDto(Guid Id, string UserName, string FullName, string Role, bool IsActive);

public sealed record CreateUserRequest(string UserName, string FullName, string Password, string Role);

public sealed record UpdateUserRequest(string FullName, string Role);

public sealed record AuditEntryDto(DateTimeOffset OccurredAt, string UserName, string Action, string Detail);
