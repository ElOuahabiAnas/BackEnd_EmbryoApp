namespace EmbryoApp.DTOs.AuthDtos;

public record UpdateStudentByIdRequest(
    string? FirstName,
    string? LastName,
    string? CodeApogee,
    string? CNE,
    string? Group
);