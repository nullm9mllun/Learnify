﻿using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Learnify.Courses;
using Learnify.Models.Enrollments;
using Learnify.Models.Students;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Students.Dtos
{
    public class StudentCourseOutput
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<EnrollmentDto> Enrollments { get; set; }
    }
}