namespace EmbryoApp.DTOs.AuthDtos;

public record StudentRegisterAutoRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string? CodeApogee,
    string? CNE,
    string? Group
);
