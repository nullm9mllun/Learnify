﻿using Abp.Dependency;
using Abp.Domain.Repositories;
using Learnify.Models.Courses;
using Learnify.Models.Students;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Courses
{
    public class CourseStepAppService : ICourseStepAppService, ITransientDependency
    {
        private readonly IRepository<CourseStep, int> _courseStepRepo;

        public CourseStepAppService(IRepository<CourseStep, int> courseStepRepo)
        {
            _courseStepRepo = courseStepRepo;
        }

        public async Task<CourseStep> CreateAsync(CourseStep courseStep)
        {
            await _courseStepRepo.InsertAsync(courseStep);
            return courseStep;
        }

        public async Task<CourseStep> DeleteAsync(int Id)
        {
            var courseStep = await _courseStepRepo.FirstOrDefaultAsync(cs => cs.Id == Id);
            if (courseStep == null)
                return null;

            await _courseStepRepo.DeleteAsync(courseStep);

            return courseStep;
        }

        public async Task<List<CourseStep>> GetCourseStepsAsync(int courseId)
        {
            var courseSteps = await _courseStepRepo
                .GetAll()
                .Where(cs => cs.CourseId == courseId)
                .ToListAsync();

            if (courseSteps.Count <= 0)
                return null;

            return courseSteps;
        }

        public async Task<CourseStep> UpdateAsync(int Id, CourseStep courseStep)
        {
            var cs = await _courseStepRepo.FirstOrDefaultAsync(cs => cs.Id == Id);

            if (cs == null) 
                return null;

            cs.StepName = courseStep.StepName;
            cs.Description = courseStep.Description;

            return cs;
        }


    }
}
