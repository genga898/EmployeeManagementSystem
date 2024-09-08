using BaseLibrary.DTOs;
using BaseLibrary.Responses;

namespace ServerLibrary.Repositories.Contracts;

public interface IUserAccount
{
    Task<GeneralResponse> CreateAsync(Register user);
    
    Task<LoginResponse> SignUpAsync(Login user);
    Task<LoginResponse> RefreshTokenAsync(RefreshToken token);
}