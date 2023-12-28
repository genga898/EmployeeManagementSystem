using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Repositories.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ServerLibrary.Repositories.Implementations;

public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext dbContext) : IUserAccount
{
    
    public async Task<GeneralResponse> CreateAsync(Register user)
    {
        if (user is null) return new GeneralResponse(false, "Model is empty");

        var checkUser = await FindUserByEmail(user.Email!);
        if (checkUser is not null) return new GeneralResponse(false, "User already registered");

        //Save user
        var applicationuser = await AddToDatabase(new ApplicationUser()
        {
            Fullname = user.Fullname,
            Email = user.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
        });
        
        //check, create and assign role
        var checkAdminRole = await dbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
        if (checkAdminRole is null)
        {
            var creatAdminRole = await AddToDatabase(new SystemRole()
            {
                Name = Constants.Admin
            });
            await AddToDatabase(new UserRole()
            {
                RoleId = creatAdminRole.Id,
                UserId = applicationuser.Id
            });
            return new GeneralResponse(true, "Account created");
        }

        var checkUserRole = await dbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.User));
        SystemRole response = new();
        if (checkUserRole is null)
        {
            response = await AddToDatabase(new SystemRole()
            {
                Name = Constants.User
            });
            await AddToDatabase(new UserRole()
            {
                RoleId = response.Id,
                UserId = applicationuser.Id
            });
        }
        else
        {
            await AddToDatabase(new UserRole()
            {
                RoleId = checkUserRole.Id,
                UserId = applicationuser.Id
            });
        }
        return new GeneralResponse(true, "Account created");
    }
    
    public async Task<LoginResponse> SignUpAsync(Login user)
    {
        if (user is null) return new LoginResponse(false, "Model is empty");

        var applicationUser = await FindUserByEmail(user.Email!);
        if (applicationUser is null) return new LoginResponse(false, "User not found");
        
        //Verify Password
        if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
            return new LoginResponse(false, "Email/Password not valid");

        var getUserRole = await FindRole(applicationUser.Id);
        if (getUserRole is null) return new LoginResponse(false, "User role not found");

        var getRoleName = await FindRoleName(getUserRole.Id);
        if (getUserRole is null) return new LoginResponse(false, "User role not found");

        string jwtToken = GenerateToken(applicationUser, getRoleName!.Name!);
        string refreshToken = GenerateRefreshToken();
        
        //Save refresh token to the database
        var findUser = await dbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == applicationUser.Id);
        if (findUser is not null)
        {
            findUser!.Token = refreshToken;
            await dbContext.SaveChangesAsync();
        }
        else
        {
            await AddToDatabase(new RefreshTokenInfo()
            {
                Token = refreshToken,
                UserId = applicationUser.Id
            });
        }
        
        return new LoginResponse(true, "Login successful", jwtToken, refreshToken);
    }

    private async Task<UserRole> FindRole(int userId) =>
        await dbContext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == userId);
    private async Task<SystemRole> FindRoleName(int roleId) =>
        await dbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Id == roleId);
    
    private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private string GenerateToken(ApplicationUser user, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var userClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Fullname),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role)
        };
        var token = new JwtSecurityToken(
            issuer: config.Value.Issuer,
            audience:config.Value.Audience,
            claims:userClaims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials:credentials
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
    {
        if (token is null) return new LoginResponse(false, "Model is empty");

        var findToken = await dbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.Token!.Equals(token.Token));
        if (findToken is null) return new LoginResponse(false, "Refresh token is required");
        
        //Get user details
        var user = await dbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == findToken.UserId);
        if (user is null)
            return new LoginResponse(false, "Refresh token could not be generated cause user was not found");

        var userRole = await FindRole(user.Id);
        var roleName = await FindRoleName(userRole.RoleId);
        string jwtToken = GenerateToken(user, roleName.Name!);
        string refreshToken = GenerateRefreshToken();

        var updateRefreshToken = await dbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == user.Id);
        if (updateRefreshToken is null)
        {
            return new LoginResponse(false, "Refresh token could not be found because user has not signed in");
        }

        updateRefreshToken.Token = refreshToken;
        await dbContext.SaveChangesAsync();
        return new LoginResponse(true, "Token added successfully", jwtToken, refreshToken);
    }
    
    private async Task<ApplicationUser> FindUserByEmail(string email) =>
        await dbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Email!.ToLower().Equals(email!.ToLower()));

    private async Task<T> AddToDatabase<T>(T model)
    {
        var result = dbContext.Add(model!);
        await dbContext.SaveChangesAsync();
        return (T)result.Entity;
    }
}