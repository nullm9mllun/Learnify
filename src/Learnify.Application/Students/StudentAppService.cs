﻿using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using Learnify.Students.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Students
{
    public class StudentAppService : LearnifyAppServiceBase ,IStudentAppService, ITransientDependency
    {
        private readonly IRepository<Student> _studnetRepo;

        public StudentAppService(IRepository<Student> StudnetRepo)
        {
            _studnetRepo = StudnetRepo;
        }

        // api Endpoint => GET: api/student/GetAll
        public async Task<StudentsOutputDto> GetAllAsync(GetAllStudentsDto input) 
        {
            var students = await _studnetRepo.GetAllListAsync();

            if(input.Name != null) 
            {
                students = await _studnetRepo.GetAllListAsync(std => std.Name.Contains(input.Name));
                
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
            var student = await _studnetRepo.FirstOrDefaultAsync(std => std.Id == input.Id);

            if(student == null)
                throw new UserFriendlyException("No Student Found!");

            return ObjectMapper.Map<StudentDto>(student);
        }

        // api Endpoint => Post: api/student/Create
        public async Task<StudentDto> CreateAsync(CreateStudentDto input)
        {
            /* TODO: Fix repetitive student avoidness
             * var stdExist = _studnetRepo.FirstOrDefault(s => s.Name == input.Name);
             if (stdExist != null)
             {
                 throw new UserFriendlyException("There is already a Student with given name");
             }*/
            var student = ObjectMapper.Map<Student>(input);
            await _studnetRepo.InsertAsync(student);
            return ObjectMapper.Map<StudentDto>(student);

        }

        // api Endpoint => Put: api/student/Update
        public async Task<StudentDto> UpdateAsync(UpdateStudentDto input) 
        {
            var student = await _studnetRepo.FirstOrDefaultAsync(std => std.Id == input.Id);
            if (student == null)
                throw new UserFriendlyException("Student Not Found!");
  
            student.Name = input.Name;
            student.Email = input.Email;

            await _studnetRepo.UpdateAsync(student);
            await CurrentUnitOfWork.SaveChangesAsync(); 

            return ObjectMapper.Map<StudentDto>(student);
        }

        // api Endpoint => Delete: api/student/Delete
        public async Task<StudentDto> DeleteAsync(GetByIdDto input) 
        {
            var student = await _studnetRepo.FirstOrDefaultAsync(std => std.Id == input.Id);
            if (student == null)
                throw new UserFriendlyException("Student Not Found!");

            await _studnetRepo.DeleteAsync(input.Id);
            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<StudentDto>(student);
        }

    }
}
