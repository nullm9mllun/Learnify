﻿using Abp.Application.Services;
using Learnify.Students.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Students
{
    public interface IStudentAppService: IApplicationService
    {
        Task<StudentsOutputDto> GetAllAsync(GetAllStudentsDto input);

        Task<StudentDto> GetByIdAsync(GetByIdDto input);

        Task<StudentCourseOutput> GetCoursesAsync(int id);

        Task<StudentProgressOutput> GetProgressAsync(int id);

        Task<StudentDto> CreateAsync(CreateStudentDto input);

        Task<StudentDto> UpdateAsync(UpdateStudentDto input);

        Task<StudentDto> DeleteAsync(GetByIdDto input);

    }
}