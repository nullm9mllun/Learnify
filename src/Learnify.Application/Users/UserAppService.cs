﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Learnify.Authorization;
using Learnify.Authorization.Accounts;
using Learnify.Authorization.Roles;
using Learnify.Authorization.Users;
using Learnify.Courses;
using Learnify.Courses.Dto;
using Learnify.Dtos.Student;
using Learnify.Enrollments;
using Learnify.Models.Courses;
using Learnify.Models.Students;
using Learnify.Roles.Dto;
using Learnify.Students;
using Learnify.Students.Dtos;
using Learnify.Users.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Users
{
    [AbpAuthorize(PermissionNames.Pages_Users)]
    public class UserAppService : AsyncCrudAppService<User, UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>, IUserAppService
    {
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IRepository<Role> _roleRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAbpSession _abpSession;
        private readonly LogInManager _logInManager;
        private readonly IStudentProgressAppService _studentProgressService;
        private readonly ICourseAppService _courseService;
        private readonly IEnrollmentAppService _enrollmentService;
        private readonly ICourseStepAppService _courseStepService;

        public UserAppService(
            IRepository<User, long> repository,
            UserManager userManager,
            RoleManager roleManager,
            IRepository<Role> roleRepository,
            IPasswordHasher<User> passwordHasher,
            IAbpSession abpSession,
            LogInManager logInManager,
            IStudentProgressAppService studentProgressAppService,
            ICourseAppService courseAppService,
            IEnrollmentAppService enrollmentAppService)
            : base(repository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _abpSession = abpSession;
            _logInManager = logInManager;
            _studentProgressService = studentProgressAppService;
            _courseService = courseAppService;
            _enrollmentService = enrollmentAppService;
        }

        public override async Task<UserDto> CreateAsync(CreateUserDto input)
        {
            CheckCreatePermission();

            var user = ObjectMapper.Map<User>(input);

            user.TenantId = AbpSession.TenantId;
            user.IsEmailConfirmed = true;

            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            CheckErrors(await _userManager.CreateAsync(user, input.Password));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));

            CurrentUnitOfWork.SaveChanges();

            return MapToEntityDto(user);
        }

        public override async Task<UserDto> UpdateAsync(UserDto input)
        {
            CheckUpdatePermission();

            var user = await _userManager.GetUserByIdAsync(input.Id);

            MapToEntity(input, user);

            CheckErrors(await _userManager.UpdateAsync(user));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            return await GetAsync(input);
        }

        public override async Task DeleteAsync(EntityDto<long> input)
        {
            var user = await _userManager.GetUserByIdAsync(input.Id);
            await _userManager.DeleteAsync(user);
        }

        /*[AbpAuthorize(PermissionNames.Pages_Users_Activation)]
        public async Task Activate(EntityDto<long> user)
        {
            await Repository.UpdateAsync(user.Id, async (entity) =>
            {
                entity.IsActive = true;
            });
        }*/

        /*[AbpAuthorize(PermissionNames.Pages_Users_Activation)]
        public async Task DeActivate(EntityDto<long> user)
        {
            await Repository.UpdateAsync(user.Id, async (entity) =>
            {
                entity.IsActive = false;
            });
        }*/

        public async Task<ListResultDto<RoleDto>> GetRoles()
        {
            var roles = await _roleRepository.GetAllListAsync();
            return new ListResultDto<RoleDto>(ObjectMapper.Map<List<RoleDto>>(roles));
        }

        /*public async Task ChangeLanguage(ChangeUserLanguageDto input)
        {
            await SettingManager.ChangeSettingForUserAsync(
                AbpSession.ToUserIdentifier(),
                LocalizationSettingNames.DefaultLanguage,
                input.LanguageName
            );
        }*/

        protected override User MapToEntity(CreateUserDto createInput)
        {
            var user = ObjectMapper.Map<User>(createInput);
            user.SetNormalizedNames();
            return user;
        }

        protected override void MapToEntity(UserDto input, User user)
        {
            ObjectMapper.Map(input, user);
            user.SetNormalizedNames();
        }

        protected override UserDto MapToEntityDto(User user)
        {
            var roleIds = user.Roles.Select(x => x.RoleId).ToArray();

            var roles = _roleManager.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.NormalizedName);

            var userDto = base.MapToEntityDto(user);
            userDto.RoleNames = roles.ToArray();

            return userDto;
        }

        protected override IQueryable<User> CreateFilteredQuery(PagedUserResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Roles)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.UserName.Contains(input.Keyword) || x.Name.Contains(input.Keyword) || x.EmailAddress.Contains(input.Keyword))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
        }

        protected override async Task<User> GetEntityByIdAsync(long id)
        {
            var user = await Repository.GetAllIncluding(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                throw new EntityNotFoundException(typeof(User), id);
            }

            return user;
        }

        protected override IQueryable<User> ApplySorting(IQueryable<User> query, PagedUserResultRequestDto input)
        {
            return query.OrderBy(r => r.UserName);
        }

        protected virtual void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        public async Task<bool> ChangePassword(ChangePasswordDto input)
        {
            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            var user = await _userManager.FindByIdAsync(AbpSession.GetUserId().ToString());
            if (user == null)
            {
                throw new Exception("There is no current user!");
            }

            if (await _userManager.CheckPasswordAsync(user, input.CurrentPassword))
            {
                CheckErrors(await _userManager.ChangePasswordAsync(user, input.NewPassword));
            }
            else
            {
                CheckErrors(IdentityResult.Failed(new IdentityError
                {
                    Description = "Incorrect password."
                }));
            }

            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attempting to reset password.");
            }

            var currentUser = await _userManager.GetUserByIdAsync(_abpSession.GetUserId());
            var loginAsync = await _logInManager.LoginAsync(currentUser.UserName, input.AdminPassword, shouldLockout: false);
            if (loginAsync.Result != AbpLoginResultType.Success)
            {
                throw new UserFriendlyException("Your 'Admin Password' did not match the one on record.  Please try again.");
            }

            if (currentUser.IsDeleted || !currentUser.IsActive)
            {
                return false;
            }

            var roles = await _userManager.GetRolesAsync(currentUser);
            if (!roles.Contains(StaticRoleNames.Tenants.Admin))
            {
                throw new UserFriendlyException("Only administrators may reset passwords.");
            }

            var user = await _userManager.GetUserByIdAsync(input.UserId);
            if (user != null)
            {
                user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return true;
        }

        // Get Students Enrolled Courses service
        public async Task<StudentCourseOutput> GetCourses(long id)
        {

            var student = await Repository
                .GetAll()
                .Include(u => u.Enrollments)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (student == null)
            {
                throw new UserFriendlyException("404, User Not Founde!");
            }

            var enrollmentsDto = student.Enrollments
                .Select(enr => new EnrollmentDto
                {
                    CourseId = enr.Course.Id,
                    CourseName = enr.Course.CourseName,
                    EnrollmentDate = enr.CreationTime
                }).ToList();

            return (new StudentCourseOutput
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.EmailAddress,
                Enrollments = enrollmentsDto
            });

        }

        public async Task<StudentProgressOutput> GetProgresses(long id)
        {

            var student = await Repository
                .GetAll()
                .Include(u => u.StudentProgresses)
                .ThenInclude(sp => sp.CourseStep)
                .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (student == null)
            {
                throw new UserFriendlyException("404, User Not Founde!");
            }

            var progressDto = student.StudentProgresses
                .Select(sp => new ProgressDto
                {
                    courseStepId = sp.CourseStepId,
                    CourseName = sp.CourseStep.Course.CourseName,
                    CourseStepName = sp.CourseStep.StepName,
                    Description = sp.CourseStep.Description,
                    State = sp.State,
                    CompletionDate = sp.CompletionDate
                }).ToList();


            return (new StudentProgressOutput
            {
                Id = student.Id,
                Name = student.Name,
                StudentProgresses = progressDto
            });

        }

        public async Task<StudentProgressOutput> UpdateStudentProgress(long id, ProgressInput input)
        {
            var student = await Repository
                .GetAll()
                .Include(u => u.StudentProgresses)
                .ThenInclude(sp => sp.CourseStep)
                .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (student == null)
            {
                throw new UserFriendlyException("404, User Not Founde!");
            }

            var stdProgressMap = ObjectMapper.Map<StudentProgress>(input);

            if (input.State == ProgressState.Completed)
            {
                stdProgressMap.CompletionDate = Clock.Now;
            }

            var result = await _studentProgressService.UpdateProgressAsync(id, stdProgressMap);

            if (result == null)
            {
                throw new UserFriendlyException("CourseStep not found or student has no progress in that.");
            }

            var progressDto = new ProgressDto
            {
                courseStepId = result.CourseStepId,
                CourseName = result.CourseStep.Course.CourseName,
                CourseStepName = result.CourseStep.StepName,
                Description = result.CourseStep.Description,
                State = result.State,
                CompletionDate = result.CompletionDate
            };

            var StudentProgresses = new List<ProgressDto>();
            StudentProgresses.Add(progressDto);

            return (new StudentProgressOutput
            {
                Id = student.Id,
                Name = student.Name,
                StudentProgresses = StudentProgresses
            });
        }

    }
}

