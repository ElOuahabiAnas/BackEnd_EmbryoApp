namespace EmbryoApp.DTOs.AuthDtos;

public record UpdateMyProfileRequest(
    string? FirstName,
    string? LastName,
    string? CodeApogee,
    string? CNE
);
