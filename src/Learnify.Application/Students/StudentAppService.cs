﻿using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.UI;
using Learnify.Students.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Students
{
    public class StudentAppService : LearnifyAppServiceBase ,IStudentAppService, ITransientDependency
    {
        private readonly IRepository<Student> _studentRepo;

        public StudentAppService(IRepository<Student> StudnetRepo)
        {
            _studentRepo = StudnetRepo;
        }

        // api Endpoint => GET: api/student/GetAll
        public async Task<StudentsOutputDto> GetAllAsync(GetAllStudentsDto input) 
        {
            var students = await _studentRepo.GetAllListAsync();

            if(!input.Name.IsNullOrWhiteSpace()) 
            {
                students = await _studentRepo.GetAllListAsync(std => std.Name.Contains(input.Name));
                
                if (students.Count == 0)
                    throw new UserFriendlyException("No Student Found!");
            }

            return new StudentsOutputDto
            {
                Students = ObjectMapper.Map<List<StudentDto>>(students)
            };
        }

        // api Endpoint => Get: api/student/GetByID
        public async Task<StudentDto> GetByIdAsync(GetByIdDto input) 
        {
            var student = await _studentRepo.FirstOrDefaultAsync(std => std.Id == input.Id);

            if(student == null)
                throw new UserFriendlyException("No Student Found!");

            return ObjectMapper.Map<StudentDto>(student);
        }

        // api Endpoint => Post: api/student/Create
        public async Task<StudentDto> CreateAsync(CreateStudentDto input)
        {
             var stdExist = _studentRepo.FirstOrDefault(s => s.Name == input.Name);
             if (stdExist != null)
             {
                 throw new UserFriendlyException("There is already a Student with given name");
             }
            var student = ObjectMapper.Map<Student>(input);
            await _studentRepo.InsertAsync(student);
            return ObjectMapper.Map<StudentDto>(student);

        }

        // api Endpoint => Put: api/student/Update
        public async Task<StudentDto> UpdateAsync(UpdateStudentDto input) 
        {
            var student = await _studentRepo.FirstOrDefaultAsync(std => std.Id == input.Id);
            if (student == null)
                throw new UserFriendlyException("Student Not Found!");
  
            student.Name = input.Name;
            student.Email = input.Email;

            await _studentRepo.UpdateAsync(student);
            await CurrentUnitOfWork.SaveChangesAsync(); 

            return ObjectMapper.Map<StudentDto>(student);
        }

        // api Endpoint => Delete: api/student/Delete
        public async Task<StudentDto> DeleteAsync(GetByIdDto input) 
        {
            var student = await _studentRepo.FirstOrDefaultAsync(std => std.Id == input.Id);
            if (student == null)
                throw new UserFriendlyException("Student Not Found!");

            await _studentRepo.DeleteAsync(input.Id);
            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<StudentDto>(student);
        }

        // api Endpoint => Get: api/student/GetCourses
        public async Task<StudentCourseOutput> GetCoursesAsync(int id)
        {
            var student = await _studentRepo
                .GetAll()
                .Include(std => std.Enrollments)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(std => std.Id == id);

            if (student == null)
                throw new UserFriendlyException("Student Not Found!");

            var enrollmentsDto = student.Enrollments
                .Select(enr => new EnrollmentDto 
                {
                    Id = enr.Id,
                    CourseId = enr.Id,
                    CourseName = enr.Course.CourseName,
                    EnrollmentDate = enr.CreationTime
                }).ToList();

            return new StudentCourseOutput {
                Name = student.Name,
                Email = student.Email,
                Enrollments = enrollmentsDto
            };

        }
        // api Endpoint => Get: api/student/GetProgress
        public async Task<StudentProgressOutput> GetProgressAsync(int id) 
        {
            var student = await _studentRepo
                .GetAll()
                .Include(std => std.StudentProgresses)
                .ThenInclude(sp => sp.CourseStep)
                .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(std => std.Id == id);

            if (student == null)
                throw new UserFriendlyException("Student Not Found!");

            var progressDto = student.StudentProgresses
                .Select(sp => new ProgressDto
                {
                    Id = sp.Id,
                    CourseName = sp.CourseStep.Course.CourseName,
                    CourseStepName = sp.CourseStep.StepName,
                    State = sp.State,
                    CompletionDate = sp.CompletionDate
                }).ToList();

            return new StudentProgressOutput
            {
                Id = student.Id,
                Name = student.Name,
                StudentProgresses = progressDto
            };
        }

    }
}