using AlAmalBusiness.Application.DTOs.Users;
using AlAmalBusiness.Application.DTOs.Users.Response;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepo _userRepo;
        private readonly IDepartmentRepo _depRepo;
        private readonly IRefreshTokenRepo _refreshTokens;
        public UserServices(IUserRepo userRepo, IDepartmentRepo departmentRepo, IRefreshTokenRepo refreshTokens)
        {
            _userRepo = userRepo;
            _depRepo = departmentRepo;
            _refreshTokens = refreshTokens;
        }

        public async Task<CreateUserResult> CreateUserAsync(CreateUserDTO user)
        {
            var department = await _depRepo.GetDepartmentByIdAsync(user.DepartmentId);
            if (department == null)
            {
                return new CreateUserResult { IsSuccess = false, Message = "Department not found." };
            }
            if (!TryNormalizeEmail(user.Email, out var email))
            {
                return new CreateUserResult { IsSuccess = false, Message = "Email address is not valid." };
            }
            var (extraDepartments, extraError) = await NormalizeExtraDepartmentsAsync(user.ExtraDepartmentIds, user.DepartmentId);
            if (extraError != null)
            {
                return new CreateUserResult { IsSuccess = false, Message = extraError };
            }
            var newUser = new User
            {
                UserName = user.UserName,
                FullName = user.FullName,
                DepartmentId = user.DepartmentId,
                Email = email,
            };

            var result = await _userRepo.CreateUserAsync(newUser, user.Password!);

            if (result.Succeeded)
            {
                var roleRes = await _userRepo.AssignRolesAsync(newUser, user.Roles);
                if (roleRes.Succeeded)
                {
                    await _userRepo.SetExtraDepartmentsAsync(newUser.Id, extraDepartments);
                    return new CreateUserResult { IsSuccess = true, Message = "Employee created successfully." };
                }
                else
                {
                    var roleErrors = string.Join(", ", roleRes.Errors.Select(e => e.Description));
                    return new CreateUserResult { IsSuccess = false, Message = $"Failed to assign roles: {roleErrors}" };
                }
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new CreateUserResult { IsSuccess = false, Message = $"Failed to create employee: {errors}" };
        }

        public async Task<List<GetUserResponse>> GetAllUserAsync() =>
            (await _userRepo.GetUserSummariesAsync()).Select(ToResponse).ToList();

        // Shared by every path that hands a user back to a caller (list,
        // get-by-id, and the response of a successful update) so
        // DepartmentId/IsActive/Roles are never forgotten on one of them —
        // GetUserResponse.DepartmentId used to be left at its default (0) on
        // every response because nothing here ever set it.
        private static GetUserResponse ToResponse(UserSummaryRow user) => new()
        {
            UserId = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            DepartmentId = user.DepartmentId,
            ExtraDepartmentIds = user.ExtraDepartmentIds,
            IsActive = user.IsActive,
            Roles = user.Roles
        };

        // The extra departments an admin may grant: each must exist, the home
        // department is dropped (it is already covered and storing it twice
        // would only make the two disagree later), and duplicates collapse.
        // An id that doesn't exist is refused rather than ignored — a silently
        // dropped grant looks exactly like a saved one on the next screen.
        private async Task<(List<int> Ids, string? Error)> NormalizeExtraDepartmentsAsync(
            List<int>? requested, int homeDepartmentId)
        {
            if (requested == null || requested.Count == 0)
                return (new List<int>(), null);

            var ids = requested
                .Where(id => id > 0 && id != homeDepartmentId)
                .Distinct()
                .ToList();

            foreach (var id in ids)
            {
                if (await _depRepo.GetDepartmentByIdAsync(id) == null)
                    return (new List<int>(), $"Department {id} not found.");
            }

            return (ids, null);
        }

        // Blank means "no email" (null), anything else must parse as a plain
        // address — no display name, since this goes straight into SMTP RCPT.
        private static bool TryNormalizeEmail(string? value, out string? email)
        {
            email = null;
            if (string.IsNullOrWhiteSpace(value)) return true;

            var trimmed = value.Trim();
            if (!System.Net.Mail.MailAddress.TryCreate(trimmed, out var parsed) || parsed.Address != trimmed)
                return false;

            email = trimmed;
            return true;
        }

        public async Task<UpdateUserResponse> ResetPasswordAsync(string id, ResetPasswordDTO updateDTO)
        {
            var resetPassword = await _userRepo.ResetPasswordAsync(id, updateDTO.Password!);

            if (resetPassword.Succeeded)
            {
                // A reset is usually "someone else may know the old password":
                // end every session that password opened, not just future ones.
                await _refreshTokens.RevokeAllForUserAsync(id);
                return new UpdateUserResponse { IsSuccess = true, Message = "Password reset successfully." };
            }
            else
            {
                var errors = string.Join(", ", resetPassword.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<UpdateUserResponse> UpdateRolesAsync(string id, UpdateUserRolesDTO updateDTO)
        {
            var updateRoles = await _userRepo.UpdateRolesAsync(id, updateDTO.Roles!);

            if (updateRoles.Succeeded)
            {
                return new UpdateUserResponse { IsSuccess = true, Message = "Employee roles updated successfully." };
            }
            else
            {
                var errors = string.Join(", ", updateRoles.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<UpdateUserResponse> UpdateUserAsync(string id, UpdateUserDto updateDTO)
        {
            var department = await _depRepo.GetDepartmentByIdAsync(updateDTO.DepartmentId);
            if (department == null)
            {
                return new UpdateUserResponse { IsSuccess = false, Message = "Department not found." };
            }

            if (!TryNormalizeEmail(updateDTO.Email, out var email))
            {
                return new UpdateUserResponse { IsSuccess = false, Message = "Email address is not valid." };
            }

            var (extraDepartments, extraError) = await NormalizeExtraDepartmentsAsync(updateDTO.ExtraDepartmentIds, updateDTO.DepartmentId);
            if (extraError != null)
            {
                return new UpdateUserResponse { IsSuccess = false, Message = extraError };
            }

            var updateUser = await _userRepo.UpdateUserAsync(id, updateDTO.UserName!, updateDTO.FullName!, updateDTO.DepartmentId, email);

            if (updateUser.Succeeded)
            {
                // Saved after the user itself so a rejected rename doesn't
                // leave the grants changed behind it. Both land in the same
                // request, and the reach on the caller's token only catches up
                // at their next refresh — the same 15 minutes a role change takes.
                await _userRepo.SetExtraDepartmentsAsync(id, extraDepartments);

                var updatedUser = await _userRepo.GetUserSummaryAsync(id);
                return new UpdateUserResponse
                {
                    IsSuccess = true,
                    User = ToResponse(updatedUser!)
                };
            }
            else
            {
                var errors = string.Join(", ", updateUser.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<UpdateUserResponse> DisableUserAsync(string id)
        {
            var disableUser = await _userRepo.DisableUserAsync(id);

            if (disableUser.Succeeded)
            {
                // Refresh already refuses an inactive account; revoking here
                // just stops the sessions now instead of at their next refresh.
                await _refreshTokens.RevokeAllForUserAsync(id);
                return new UpdateUserResponse { IsSuccess = true, Message = "Employee disabled successfully." };
            }
            else
            {
                var errors = string.Join(", ", disableUser.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<UpdateUserResponse> EnableUserAsync(string id)
        {
            var enableUser = await _userRepo.EnableUserAsync(id);

            if (enableUser.Succeeded)
            {
                return new UpdateUserResponse { IsSuccess = true, Message = "Employee enabled successfully." };
            }
            else
            {
                var errors = string.Join(", ", enableUser.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<GetUserResponse> GetUserById(string id)
        {
            var user = await _userRepo.GetUserSummaryAsync(id);
            return user == null ? null! : ToResponse(user);
        }

      
    }
}