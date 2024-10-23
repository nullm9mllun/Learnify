﻿using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using Learnify.Enrollments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Courses
{
    public class Course : Entity, IHasCreationTime
    {
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public DateTime CreationTime { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<CourseStep> CourseSteps { get; set; }

        public Course()
        {
            CreationTime = Clock.Now;
        }
    }
}